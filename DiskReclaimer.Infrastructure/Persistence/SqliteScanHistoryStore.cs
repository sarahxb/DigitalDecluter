using Dapper;
using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;
using Microsoft.Data.Sqlite;

namespace DiskReclaimer.Infrastructure.Persistence;

/// <summary>
/// Records each completed scan's summary to a local SQLite database so past runs can be browsed later.
/// Timestamps are stored as ISO 8601 text (SQLite has no native temporal type, and Dapper has no
/// built-in DateTimeOffset conversion) and parsed back on the way out.
/// </summary>
public sealed class SqliteScanHistoryStore : IScanHistoryStore
{
    private readonly string _connectionString;

    public SqliteScanHistoryStore(string databaseFilePath)
    {
        var directory = Path.GetDirectoryName(databaseFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Pooling off: a pooled connection keeps the native SQLite handle alive after Dispose(), which
        // holds the file locked — harmless in the running app, but it breaks tests that delete the
        // database file immediately after use.
        _connectionString = $"Data Source={databaseFilePath};Pooling=False";
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS ScanRuns (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RootPath TEXT NOT NULL,
                ScanStartedUtc TEXT NOT NULL,
                ScanCompletedUtc TEXT NOT NULL,
                FilesScanned INTEGER NOT NULL,
                FoldersDetected INTEGER NOT NULL,
                RecommendationCount INTEGER NOT NULL,
                TotalReclaimableBytes INTEGER NOT NULL
            );
            """);
    }

    public async Task<ScanHistoryEntry> RecordScanAsync(ScanHistoryEntry entry, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);

        var id = await connection.ExecuteScalarAsync<long>(new CommandDefinition("""
            INSERT INTO ScanRuns (RootPath, ScanStartedUtc, ScanCompletedUtc, FilesScanned, FoldersDetected, RecommendationCount, TotalReclaimableBytes)
            VALUES (@RootPath, @ScanStartedUtc, @ScanCompletedUtc, @FilesScanned, @FoldersDetected, @RecommendationCount, @TotalReclaimableBytes);
            SELECT last_insert_rowid();
            """,
            new
            {
                entry.RootPath,
                ScanStartedUtc = entry.ScanStartedUtc.ToString("O"),
                ScanCompletedUtc = entry.ScanCompletedUtc.ToString("O"),
                entry.FilesScanned,
                entry.FoldersDetected,
                entry.RecommendationCount,
                entry.TotalReclaimableBytes
            },
            cancellationToken: cancellationToken));

        return entry with { Id = id };
    }

    public async Task<IReadOnlyList<ScanHistoryEntry>> GetRecentScansAsync(int limit, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);

        var rows = await connection.QueryAsync<ScanRunRow>(new CommandDefinition("""
            SELECT Id, RootPath, ScanStartedUtc, ScanCompletedUtc, FilesScanned, FoldersDetected, RecommendationCount, TotalReclaimableBytes
            FROM ScanRuns
            ORDER BY ScanCompletedUtc DESC
            LIMIT @Limit;
            """,
            new { Limit = limit },
            cancellationToken: cancellationToken));

        return rows.Select(row => new ScanHistoryEntry(
            row.Id,
            row.RootPath,
            DateTimeOffset.Parse(row.ScanStartedUtc),
            DateTimeOffset.Parse(row.ScanCompletedUtc),
            (int)row.FilesScanned,
            (int)row.FoldersDetected,
            (int)row.RecommendationCount,
            row.TotalReclaimableBytes)).ToList();
    }

    // SQLite's only integer storage class comes back through Microsoft.Data.Sqlite as Int64 regardless
    // of the declared column width, and Dapper's constructor-matching needs the row type's fields to
    // line up exactly with that — hence long here even though ScanHistoryEntry exposes int.
    private sealed record ScanRunRow(
        long Id,
        string RootPath,
        string ScanStartedUtc,
        string ScanCompletedUtc,
        long FilesScanned,
        long FoldersDetected,
        long RecommendationCount,
        long TotalReclaimableBytes);
}
