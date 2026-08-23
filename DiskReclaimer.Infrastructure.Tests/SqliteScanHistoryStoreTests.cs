using DiskReclaimer.Infrastructure.Persistence;

namespace DiskReclaimer.Infrastructure.Tests;

public sealed class SqliteScanHistoryStoreTests : IDisposable
{
    private readonly string _databaseFilePath;

    public SqliteScanHistoryStoreTests()
    {
        _databaseFilePath = Path.Combine(Path.GetTempPath(), "DiskReclaimerTests_" + Guid.NewGuid() + ".db");
    }

    public void Dispose()
    {
        if (File.Exists(_databaseFilePath))
        {
            File.Delete(_databaseFilePath);
        }
    }

    private static Core.Models.ScanHistoryEntry MakeEntry(string rootPath, DateTimeOffset completedUtc, long reclaimableBytes = 1000) =>
        new(
            Id: 0,
            RootPath: rootPath,
            ScanStartedUtc: completedUtc.AddSeconds(-30),
            ScanCompletedUtc: completedUtc,
            FilesScanned: 10,
            FoldersDetected: 2,
            RecommendationCount: 3,
            TotalReclaimableBytes: reclaimableBytes);

    [Fact]
    public void Constructor_CreatesDatabaseFile_AndSchema()
    {
        _ = new SqliteScanHistoryStore(_databaseFilePath);

        Assert.True(File.Exists(_databaseFilePath));
    }

    [Fact]
    public async Task RecordScanAsync_AssignsAnId()
    {
        var store = new SqliteScanHistoryStore(_databaseFilePath);
        var entry = MakeEntry(@"C:\data", DateTimeOffset.UtcNow);

        var recorded = await store.RecordScanAsync(entry, CancellationToken.None);

        Assert.True(recorded.Id > 0);
    }

    [Fact]
    public async Task GetRecentScansAsync_ReturnsRecordedEntry_WithAllFieldsIntact()
    {
        var store = new SqliteScanHistoryStore(_databaseFilePath);
        var now = DateTimeOffset.UtcNow;
        var entry = MakeEntry(@"C:\projects", now, reclaimableBytes: 123_456_789);
        await store.RecordScanAsync(entry, CancellationToken.None);

        var results = await store.GetRecentScansAsync(10, CancellationToken.None);

        var stored = Assert.Single(results);
        Assert.Equal(@"C:\projects", stored.RootPath);
        Assert.Equal(10, stored.FilesScanned);
        Assert.Equal(2, stored.FoldersDetected);
        Assert.Equal(3, stored.RecommendationCount);
        Assert.Equal(123_456_789, stored.TotalReclaimableBytes);
        Assert.Equal(entry.ScanCompletedUtc.ToString("O"), stored.ScanCompletedUtc.ToString("O"));
        Assert.Equal(entry.ScanStartedUtc.ToString("O"), stored.ScanStartedUtc.ToString("O"));
    }

    [Fact]
    public async Task GetRecentScansAsync_OrdersByCompletedTimeDescending()
    {
        var store = new SqliteScanHistoryStore(_databaseFilePath);
        var now = DateTimeOffset.UtcNow;
        await store.RecordScanAsync(MakeEntry(@"C:\oldest", now.AddDays(-2)), CancellationToken.None);
        await store.RecordScanAsync(MakeEntry(@"C:\newest", now), CancellationToken.None);
        await store.RecordScanAsync(MakeEntry(@"C:\middle", now.AddDays(-1)), CancellationToken.None);

        var results = await store.GetRecentScansAsync(10, CancellationToken.None);

        Assert.Equal([@"C:\newest", @"C:\middle", @"C:\oldest"], results.Select(r => r.RootPath));
    }

    [Fact]
    public async Task GetRecentScansAsync_RespectsLimit()
    {
        var store = new SqliteScanHistoryStore(_databaseFilePath);
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 5; i++)
        {
            await store.RecordScanAsync(MakeEntry($@"C:\scan{i}", now.AddMinutes(i)), CancellationToken.None);
        }

        var results = await store.GetRecentScansAsync(2, CancellationToken.None);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetRecentScansAsync_ReturnsEmpty_WhenNothingRecorded()
    {
        var store = new SqliteScanHistoryStore(_databaseFilePath);

        var results = await store.GetRecentScansAsync(10, CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task Store_PersistsAcrossNewConnections_ToSameDatabaseFile()
    {
        var writer = new SqliteScanHistoryStore(_databaseFilePath);
        await writer.RecordScanAsync(MakeEntry(@"C:\data", DateTimeOffset.UtcNow), CancellationToken.None);

        var reader = new SqliteScanHistoryStore(_databaseFilePath);
        var results = await reader.GetRecentScansAsync(10, CancellationToken.None);

        Assert.Single(results);
    }
}
