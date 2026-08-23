using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Recommendations;

/// <summary>
/// The only place that turns raw Findings into scored, prioritized Recommendations. Merges every
/// finding reported against the same target path so a file flagged by multiple detectors (large AND
/// stale, say) becomes one recommendation instead of several — and the number of detectors that
/// independently agree on a target is itself the confidence signal.
/// </summary>
public sealed class RecommendationEngine : IRecommendationEngine
{
    public IReadOnlyList<Recommendation> BuildRecommendations(IReadOnlyList<Finding> findings)
    {
        var recommendations = new List<Recommendation>();

        foreach (var group in findings.GroupBy(f => f.TargetPath))
        {
            var groupFindings = group.ToList();
            var reclaimableBytes = groupFindings.Max(f => f.SizeBytes);
            var distinctDetectorCount = groupFindings.Select(f => f.DetectorName).Distinct().Count();
            var reasons = groupFindings.Select(f => f.Description).ToList();

            recommendations.Add(new Recommendation(
                group.Key,
                ConfidenceTierFor(distinctDetectorCount),
                reclaimableBytes,
                reasons,
                groupFindings));
        }

        return recommendations.OrderByDescending(r => r.ReclaimableBytes).ToList();
    }

    private static ConfidenceTier ConfidenceTierFor(int distinctDetectorCount) => distinctDetectorCount switch
    {
        <= 1 => ConfidenceTier.Low,
        2 => ConfidenceTier.Medium,
        _ => ConfidenceTier.High
    };
}
