using DiskReclaimer.Application.Categorization;
using DiskReclaimer.Application.Categorization.FolderPatternRules;
using DiskReclaimer.Application.Detectors;
using DiskReclaimer.Application.Insights;
using DiskReclaimer.Application.Recommendations;
using DiskReclaimer.Application.Scanning;
using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace DiskReclaimer.Application.Tests;

public sealed class ScanOrchestratorTests
{
    private sealed class FakeFileScanner(IReadOnlyList<FileRecord> files) : IFileScanner
    {
        public Task<IReadOnlyList<FileRecord>> ScanAsync(string rootPath, IProgress<ScanProgress>? progress, CancellationToken cancellationToken) =>
            Task.FromResult(files);
    }

    private sealed class FakeExclusionRuleProvider(IReadOnlyList<ExclusionRule> rules) : IExclusionRuleProvider
    {
        public Task<IReadOnlyList<ExclusionRule>> GetRulesAsync() => Task.FromResult(rules);

        public Task AddUserRuleAsync(ExclusionRule rule, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RemoveUserRuleAsync(string pathPattern, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeScanHistoryStore : IScanHistoryStore
    {
        public List<ScanHistoryEntry> RecordedEntries { get; } = [];

        public Task<ScanHistoryEntry> RecordScanAsync(ScanHistoryEntry entry, CancellationToken cancellationToken)
        {
            RecordedEntries.Add(entry);
            return Task.FromResult(entry with { Id = RecordedEntries.Count });
        }

        public Task<IReadOnlyList<ScanHistoryEntry>> GetRecentScansAsync(int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ScanHistoryEntry>>(RecordedEntries);
    }

    private sealed class ThrowingScanHistoryStore : IScanHistoryStore
    {
        public Task<ScanHistoryEntry> RecordScanAsync(ScanHistoryEntry entry, CancellationToken cancellationToken) =>
            throw new IOException("disk full");

        public Task<IReadOnlyList<ScanHistoryEntry>> GetRecentScansAsync(int limit, CancellationToken cancellationToken) =>
            throw new IOException("disk full");
    }

    private static FileRecord MakeFile(string path, long sizeBytes)
    {
        var now = DateTimeOffset.UtcNow;
        return new FileRecord(path, Path.GetFileName(path), Path.GetExtension(path), sizeBytes, now, now, now);
    }

    private static ScanOrchestrator BuildOrchestrator(
        IReadOnlyList<FileRecord> files,
        IReadOnlyList<ExclusionRule> exclusionRules,
        long largeFileThresholdBytes,
        IScanHistoryStore scanHistoryStore,
        Categorizer? categorizer = null) =>
        new(
            new FakeFileScanner(files),
            categorizer ?? new Categorizer([], []),
            new FakeExclusionRuleProvider(exclusionRules),
            [new LargeFileDetector(largeFileThresholdBytes)],
            new RecommendationEngine(),
            new FolderInsightsService(),
            scanHistoryStore,
            NullLogger<ScanOrchestrator>.Instance);

    [Fact]
    public async Task ScanAsync_ProducesRecommendations_ForLargeFiles_EndToEnd()
    {
        var files = new[]
        {
            MakeFile(@"C:\data\huge.bin", 1_000_000),
            MakeFile(@"C:\data\small.txt", 10)
        };

        var orchestrator = BuildOrchestrator(files, [], 1000, new FakeScanHistoryStore());

        var result = await orchestrator.ScanAsync(@"C:\data", null, CancellationToken.None);

        Assert.Equal(2, result.Categorization.Files.Count);
        var recommendation = Assert.Single(result.Recommendations);
        Assert.Equal(@"C:\data\huge.bin", recommendation.TargetPath);
        Assert.Equal(1_000_000, recommendation.ReclaimableBytes);
    }

    [Fact]
    public async Task ScanAsync_ExcludesFilesBeforeRunningDetectors()
    {
        var files = new[] { MakeFile(@"C:\Windows\huge.bin", 1_000_000) };
        var rules = new[] { new ExclusionRule(@"C:\Windows", "system", IsSystemFloor: true) };

        var orchestrator = BuildOrchestrator(files, rules, 1000, new FakeScanHistoryStore());

        var result = await orchestrator.ScanAsync(@"C:\Windows", null, CancellationToken.None);

        Assert.Empty(result.Categorization.Files);
        Assert.Empty(result.Recommendations);
    }

    [Fact]
    public async Task ScanAsync_RecordsScanHistoryEntry_WithSummaryStats()
    {
        var files = new[]
        {
            MakeFile(@"C:\data\huge.bin", 1_000_000),
            MakeFile(@"C:\data\small.txt", 10)
        };
        var historyStore = new FakeScanHistoryStore();

        var orchestrator = BuildOrchestrator(files, [], 1000, historyStore);
        await orchestrator.ScanAsync(@"C:\data", null, CancellationToken.None);

        var entry = Assert.Single(historyStore.RecordedEntries);
        Assert.Equal(@"C:\data", entry.RootPath);
        Assert.Equal(2, entry.FilesScanned);
        Assert.Equal(0, entry.FoldersDetected);
        Assert.Equal(1, entry.RecommendationCount);
        Assert.Equal(1_000_000, entry.TotalReclaimableBytes);
        Assert.True(entry.ScanCompletedUtc >= entry.ScanStartedUtc);
    }

    [Fact]
    public async Task ScanAsync_ProducesInsights_ForDetectedFolders_EndToEnd()
    {
        var files = new[] { MakeFile(@"C:\proj\node_modules\pkg\index.js", 500) };
        var categorizer = new Categorizer([], [new NodeModulesFolderPatternRule()]);

        var orchestrator = BuildOrchestrator(files, [], 1000, new FakeScanHistoryStore(), categorizer);
        var result = await orchestrator.ScanAsync(@"C:\proj", null, CancellationToken.None);

        var insight = Assert.Single(result.Insights);
        Assert.Equal(@"C:\proj\node_modules", insight.FolderPath);
        Assert.Equal(500, insight.TotalSizeBytes);
        Assert.Equal(1, insight.FileCount);
    }

    [Fact]
    public async Task ScanAsync_StillReturnsResult_WhenHistoryStoreFails()
    {
        var files = new[] { MakeFile(@"C:\data\huge.bin", 1_000_000) };

        var orchestrator = BuildOrchestrator(files, [], 1000, new ThrowingScanHistoryStore());

        var result = await orchestrator.ScanAsync(@"C:\data", null, CancellationToken.None);

        Assert.Single(result.Recommendations);
    }
}
