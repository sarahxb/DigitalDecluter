namespace DiskReclaimer.Core.Models;

/// <summary>Periodic progress signal during a scan, for a long-running scan to stay visibly alive in the UI.</summary>
public sealed record ScanProgress(int FilesScanned, string? CurrentDirectory);
