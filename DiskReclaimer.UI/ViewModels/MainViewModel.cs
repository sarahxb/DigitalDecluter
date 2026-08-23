using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskReclaimer.Application.Export;
using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;
using Microsoft.Extensions.Logging;

namespace DiskReclaimer.UI.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IScanOrchestrator _scanOrchestrator;
    private readonly IScanHistoryStore _scanHistoryStore;
    private readonly ILogger<MainViewModel> _logger;
    private CancellationTokenSource? _scanCancellation;

    [ObservableProperty]
    private string _rootPath = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Choose a folder and click Scan.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    private bool _isScanning;

    public ObservableCollection<CategorizedFile> Files { get; } = [];

    public ObservableCollection<DetectedFolder> Folders { get; } = [];

    public ObservableCollection<Recommendation> Recommendations { get; } = [];

    public ObservableCollection<InsightSummary> Insights { get; } = [];

    public ObservableCollection<ScanHistoryEntry> History { get; } = [];

    public MainViewModel(IScanOrchestrator scanOrchestrator, IScanHistoryStore scanHistoryStore, ILogger<MainViewModel> logger)
    {
        _scanOrchestrator = scanOrchestrator;
        _scanHistoryStore = scanHistoryStore;
        _logger = logger;
    }

    /// <summary>Loads scan history for display; called once the window is up so a slow DB read never blocks startup.</summary>
    public async Task InitializeAsync() => await RefreshHistoryAsync();

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        if (string.IsNullOrWhiteSpace(RootPath) || !Directory.Exists(RootPath))
        {
            StatusMessage = "Choose a valid folder first.";
            return;
        }

        Files.Clear();
        Folders.Clear();
        Recommendations.Clear();
        Insights.Clear();
        IsScanning = true;
        StatusMessage = $"Scanning {RootPath}...";
        _scanCancellation = new CancellationTokenSource();

        try
        {
            var result = await _scanOrchestrator.ScanAsync(RootPath, _scanCancellation.Token);

            foreach (var file in result.Categorization.Files)
            {
                Files.Add(file);
            }

            foreach (var folder in result.Categorization.Folders)
            {
                Folders.Add(folder);
            }

            foreach (var recommendation in result.Recommendations)
            {
                Recommendations.Add(recommendation);
            }

            foreach (var insight in result.Insights)
            {
                Insights.Add(insight);
            }

            await RefreshHistoryAsync();

            StatusMessage = $"Scan complete: {result.Categorization.Files.Count} files, " +
                $"{result.Categorization.Folders.Count} detected folders, {result.Recommendations.Count} recommendations.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan failed for {RootPath}", RootPath);
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            _scanCancellation = null;
        }
    }

    private bool CanScan() => !IsScanning;

    private async Task RefreshHistoryAsync()
    {
        try
        {
            var entries = await _scanHistoryStore.GetRecentScansAsync(limit: 50, CancellationToken.None);
            History.Clear();
            foreach (var entry in entries)
            {
                History.Add(entry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load scan history");
        }
    }

    [RelayCommand]
    private void CancelScan() => _scanCancellation?.Cancel();

    public void ExportRecommendationsToCsv(string filePath)
    {
        try
        {
            var csv = RecommendationCsvExporter.Write(Recommendations);
            File.WriteAllText(filePath, csv);
            StatusMessage = $"Exported {Recommendations.Count} recommendation(s) to {filePath}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export recommendations to {FilePath}", filePath);
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RevealInExplorer(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var argument = BuildRevealArgument(path);
        if (argument is null)
        {
            StatusMessage = $"Can't reveal — path no longer exists: {path}";
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo("explorer.exe") { UseShellExecute = true };
            startInfo.ArgumentList.Add(argument);
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reveal {Path} in Explorer", path);
            StatusMessage = $"Failed to open Explorer: {ex.Message}";
        }
    }

    /// <summary>
    /// A file gets selected with its parent folder open (Explorer's /select switch); a folder is opened
    /// directly. Either can have vanished since the scan ran, so both are checked — null means neither
    /// exists any more and there's nothing left to reveal.
    /// </summary>
    public static string? BuildRevealArgument(string path)
    {
        if (File.Exists(path))
        {
            return $"/select,{path}";
        }

        if (Directory.Exists(path))
        {
            return path;
        }

        return null;
    }
}
