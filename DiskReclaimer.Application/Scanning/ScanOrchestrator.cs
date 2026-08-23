using DiskReclaimer.Application.Exclusions;
using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;
using Microsoft.Extensions.Logging;

namespace DiskReclaimer.Application.Scanning;

/// <summary>
/// Wires Scanner → Categorizer → exclusion filter → Detectors + FolderInsightsService (run in
/// parallel, both consuming the same scoped index) → RecommendationEngine into a single call for the
/// UI to drive, then records a summary of the run to scan history.
/// </summary>
public sealed class ScanOrchestrator : IScanOrchestrator
{
    private readonly IFileScanner _fileScanner;
    private readonly ICategorizer _categorizer;
    private readonly IExclusionRuleProvider _exclusionRuleProvider;
    private readonly IReadOnlyList<IRecommendationDetector> _detectors;
    private readonly IRecommendationEngine _recommendationEngine;
    private readonly IFolderInsightsService _folderInsightsService;
    private readonly IScanHistoryStore _scanHistoryStore;
    private readonly ILogger<ScanOrchestrator> _logger;

    public ScanOrchestrator(
        IFileScanner fileScanner,
        ICategorizer categorizer,
        IExclusionRuleProvider exclusionRuleProvider,
        IReadOnlyList<IRecommendationDetector> detectors,
        IRecommendationEngine recommendationEngine,
        IFolderInsightsService folderInsightsService,
        IScanHistoryStore scanHistoryStore,
        ILogger<ScanOrchestrator> logger)
    {
        _fileScanner = fileScanner;
        _categorizer = categorizer;
        _exclusionRuleProvider = exclusionRuleProvider;
        _detectors = detectors;
        _recommendationEngine = recommendationEngine;
        _folderInsightsService = folderInsightsService;
        _scanHistoryStore = scanHistoryStore;
        _logger = logger;
    }

    public async Task<ScanResult> ScanAsync(string rootPath, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        var scanStartedUtc = DateTimeOffset.UtcNow;

        var files = await _fileScanner.ScanAsync(rootPath, progress, cancellationToken);
        var categorized = _categorizer.Categorize(files);
        var exclusionRules = await _exclusionRuleProvider.GetRulesAsync();
        var scoped = ExclusionFilter.Apply(categorized, exclusionRules);

        var findings = _detectors
            .SelectMany(detector => detector.Detect(scoped.Files, scoped.Folders))
            .ToList();
        var recommendations = _recommendationEngine.BuildRecommendations(findings);
        var insights = _folderInsightsService.Summarize(scoped.Files, scoped.Folders);

        await RecordHistoryAsync(rootPath, scanStartedUtc, scoped, recommendations, cancellationToken);

        return new ScanResult(scoped, recommendations, insights);
    }

    /// <summary>A history-write failure shouldn't fail the scan the user is waiting on — log and move on.</summary>
    private async Task RecordHistoryAsync(
        string rootPath,
        DateTimeOffset scanStartedUtc,
        CategorizationResult scoped,
        IReadOnlyList<Recommendation> recommendations,
        CancellationToken cancellationToken)
    {
        try
        {
            var entry = new ScanHistoryEntry(
                Id: 0,
                RootPath: rootPath,
                ScanStartedUtc: scanStartedUtc,
                ScanCompletedUtc: DateTimeOffset.UtcNow,
                FilesScanned: scoped.Files.Count,
                FoldersDetected: scoped.Folders.Count,
                RecommendationCount: recommendations.Count,
                TotalReclaimableBytes: recommendations.Sum(r => r.ReclaimableBytes));

            await _scanHistoryStore.RecordScanAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record scan history for {RootPath}", rootPath);
        }
    }
}
