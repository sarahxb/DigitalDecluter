using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Detectors;

/// <summary>
/// Flags files that haven't been touched in a long time. "Touched" is the more recent of last-modified
/// and last-accessed, not last-accessed alone — NTFS last-access-time tracking is disabled by default
/// on many Windows systems, so relying on it exclusively would make every old-but-untracked file look
/// stale the moment it was created.
/// </summary>
public sealed class StaleFileDetector : IRecommendationDetector
{
    public static readonly TimeSpan DefaultStaleThreshold = TimeSpan.FromDays(180);

    private readonly TimeSpan _staleThreshold;
    private readonly Func<DateTimeOffset> _nowProvider;

    public StaleFileDetector(TimeSpan? staleThreshold = null, Func<DateTimeOffset>? nowProvider = null)
    {
        _staleThreshold = staleThreshold ?? DefaultStaleThreshold;
        _nowProvider = nowProvider ?? (() => DateTimeOffset.UtcNow);
    }

    public IEnumerable<Finding> Detect(IReadOnlyList<CategorizedFile> files, IReadOnlyList<DetectedFolder> folders)
    {
        var now = _nowProvider();

        foreach (var file in files)
        {
            var lastTouchedUtc = file.Record.LastModifiedUtc > file.Record.LastAccessedUtc
                ? file.Record.LastModifiedUtc
                : file.Record.LastAccessedUtc;

            var age = now - lastTouchedUtc;
            if (age >= _staleThreshold)
            {
                yield return new Finding(
                    file.Record.FullPath,
                    nameof(StaleFileDetector),
                    $"Not used in {(int)age.TotalDays} days (last touched {lastTouchedUtc:yyyy-MM-dd})",
                    file.Record.SizeBytes);
            }
        }
    }
}
