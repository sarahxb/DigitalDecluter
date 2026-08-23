namespace DiskReclaimer.Core.Models;

public sealed record ScanHistoryEntry(
    long Id,
    string RootPath,
    DateTimeOffset ScanStartedUtc,
    DateTimeOffset ScanCompletedUtc,
    int FilesScanned,
    int FoldersDetected,
    int RecommendationCount,
    long TotalReclaimableBytes);
