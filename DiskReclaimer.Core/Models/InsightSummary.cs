namespace DiskReclaimer.Core.Models;

public sealed record InsightSummary(
    string FolderPath,
    long TotalSizeBytes,
    int FileCount,
    IReadOnlyDictionary<Category, long> CategoryBreakdown);
