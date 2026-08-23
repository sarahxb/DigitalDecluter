using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Core.Interfaces;

public interface IFolderInsightsService
{
    IReadOnlyList<InsightSummary> Summarize(IReadOnlyList<CategorizedFile> files, IReadOnlyList<DetectedFolder> folders);
}
