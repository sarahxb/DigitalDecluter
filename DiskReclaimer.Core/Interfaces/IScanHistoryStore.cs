using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Core.Interfaces;

public interface IScanHistoryStore
{
    /// <summary>Persists a completed scan's summary and returns it with its assigned Id.</summary>
    Task<ScanHistoryEntry> RecordScanAsync(ScanHistoryEntry entry, CancellationToken cancellationToken);

    /// <summary>Most recent scans first, newest-completed-first, capped at <paramref name="limit"/>.</summary>
    Task<IReadOnlyList<ScanHistoryEntry>> GetRecentScansAsync(int limit, CancellationToken cancellationToken);
}
