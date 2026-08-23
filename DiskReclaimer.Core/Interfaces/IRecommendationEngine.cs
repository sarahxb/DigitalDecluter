using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Core.Interfaces;

public interface IRecommendationEngine
{
    IReadOnlyList<Recommendation> BuildRecommendations(IReadOnlyList<Finding> findings);
}
