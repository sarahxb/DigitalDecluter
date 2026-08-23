using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Detectors;

/// <summary>Flags any file at or above a size threshold. Reports raw findings only — no scoring.</summary>
public sealed class LargeFileDetector : IRecommendationDetector
{
    public const long DefaultThresholdBytes = 500L * 1024 * 1024;

    private readonly long _thresholdBytes;

    public LargeFileDetector(long thresholdBytes = DefaultThresholdBytes)
    {
        _thresholdBytes = thresholdBytes;
    }

    public IEnumerable<Finding> Detect(IReadOnlyList<CategorizedFile> files, IReadOnlyList<DetectedFolder> folders)
    {
        foreach (var file in files)
        {
            if (file.Record.SizeBytes >= _thresholdBytes)
            {
                yield return new Finding(
                    file.Record.FullPath,
                    nameof(LargeFileDetector),
                    $"Large file ({FormatBytes(file.Record.SizeBytes)})",
                    file.Record.SizeBytes);
            }
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.#} {units[unitIndex]}";
    }
}
