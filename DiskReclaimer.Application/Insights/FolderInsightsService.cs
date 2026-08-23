using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Insights;

/// <summary>
/// A second, purely descriptive consumer of Categorizer's output, run in parallel with Detectors:
/// where Detectors answer "what should I do", this answers "what do I have" — one summary per
/// detected project/junk folder, breaking down its contents by category.
/// </summary>
public sealed class FolderInsightsService : IFolderInsightsService
{
    public IReadOnlyList<InsightSummary> Summarize(IReadOnlyList<CategorizedFile> files, IReadOnlyList<DetectedFolder> folders)
    {
        var summaries = new List<InsightSummary>(folders.Count);

        foreach (var folder in folders)
        {
            var categoryBreakdown = FilesUnder(files, folder.Path)
                .GroupBy(f => f.Category)
                .ToDictionary(g => g.Key, g => g.Sum(f => f.Record.SizeBytes));

            // TotalSizeBytes/FileCount come from Categorizer's own aggregation rather than being
            // recomputed here, so an insight and its detected folder never disagree on the numbers.
            summaries.Add(new InsightSummary(folder.Path, folder.AggregateSizeBytes, folder.FileCount, categoryBreakdown));
        }

        return summaries;
    }

    private static List<CategorizedFile> FilesUnder(IReadOnlyList<CategorizedFile> files, string folderPath)
    {
        var prefix = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return files.Where(f => f.Record.FullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
