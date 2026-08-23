using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
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
    private readonly IExclusionRuleProvider _exclusionRuleProvider;
    private readonly IRecycleBinService _recycleBinService;
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

    public ObservableCollection<ExclusionRule> ExclusionRules { get; } = [];

    [ObservableProperty]
    private string _newExclusionPath = string.Empty;

    [ObservableProperty]
    private string _newExclusionReason = string.Empty;

    public MainViewModel(
        IScanOrchestrator scanOrchestrator,
        IScanHistoryStore scanHistoryStore,
        IExclusionRuleProvider exclusionRuleProvider,
        IRecycleBinService recycleBinService,
        ILogger<MainViewModel> logger)
    {
        _scanOrchestrator = scanOrchestrator;
        _scanHistoryStore = scanHistoryStore;
        _exclusionRuleProvider = exclusionRuleProvider;
        _recycleBinService = recycleBinService;
        _logger = logger;
    }

    /// <summary>Loads scan history and exclusion rules for display; called once the window is up so a slow read never blocks startup.</summary>
    public async Task InitializeAsync()
    {
        await RefreshHistoryAsync();
        await RefreshExclusionRulesAsync();
    }

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

    [RelayCommand]
    private async Task AddExclusionRuleAsync()
    {
        if (string.IsNullOrWhiteSpace(NewExclusionPath))
        {
            StatusMessage = "Enter a path or pattern to exclude first.";
            return;
        }

        try
        {
            var reason = string.IsNullOrWhiteSpace(NewExclusionReason) ? "User-defined exclusion" : NewExclusionReason;
            var rule = new ExclusionRule(NewExclusionPath, reason, IsSystemFloor: false);
            await _exclusionRuleProvider.AddUserRuleAsync(rule, CancellationToken.None);

            NewExclusionPath = string.Empty;
            NewExclusionReason = string.Empty;
            await RefreshExclusionRulesAsync();
            StatusMessage = $"Added exclusion rule for {rule.PathPattern}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add exclusion rule for {PathPattern}", NewExclusionPath);
            StatusMessage = $"Failed to add exclusion rule: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RemoveExclusionRuleAsync(ExclusionRule? rule)
    {
        if (rule is null || rule.IsSystemFloor)
        {
            return;
        }

        try
        {
            await _exclusionRuleProvider.RemoveUserRuleAsync(rule.PathPattern, CancellationToken.None);
            await RefreshExclusionRulesAsync();
            StatusMessage = $"Removed exclusion rule for {rule.PathPattern}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove exclusion rule for {PathPattern}", rule.PathPattern);
            StatusMessage = $"Failed to remove exclusion rule: {ex.Message}";
        }
    }

    private async Task RefreshExclusionRulesAsync()
    {
        try
        {
            var rules = await _exclusionRuleProvider.GetRulesAsync();
            ExclusionRules.Clear();
            foreach (var rule in rules)
            {
                ExclusionRules.Add(rule);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load exclusion rules");
        }
    }

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

    [RelayCommand]
    private async Task DeleteRecommendationAsync(Recommendation? recommendation)
    {
        if (recommendation is null)
        {
            return;
        }

        var confirmed = MessageBox.Show(
            $"Move this to the Recycle Bin?\n\n{recommendation.TargetPath}\n\n" +
            $"Estimated space reclaimed: {recommendation.ReclaimableBytes:N0} bytes.",
            "Confirm delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes;

        if (!confirmed)
        {
            return;
        }

        try
        {
            var deleted = await _recycleBinService.DeleteAsync(recommendation.TargetPath, CancellationToken.None);
            if (deleted)
            {
                Recommendations.Remove(recommendation);
                StatusMessage = $"Moved to Recycle Bin: {recommendation.TargetPath}";
            }
            else
            {
                StatusMessage = $"Nothing to delete — {recommendation.TargetPath} no longer exists.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete {Path}", recommendation.TargetPath);
            StatusMessage = $"Failed to delete: {ex.Message}";
        }
    }
}
