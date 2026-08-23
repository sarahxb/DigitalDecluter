using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Core.Interfaces;

public interface IRecommendationDetector
{
    IEnumerable<Finding> Detect(IReadOnlyList<CategorizedFile> files, IReadOnlyList<DetectedFolder> folders);
}
