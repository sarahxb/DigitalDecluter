using DiskReclaimer.Application.Recommendations;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Tests;

public sealed class RecommendationEngineTests
{
    [Fact]
    public void BuildRecommendations_ReturnsOneRecommendationPerTarget()
    {
        var engine = new RecommendationEngine();
        var findings = new[]
        {
            new Finding(@"C:\data\a.bin", "LargeFileDetector", "Large file (1 GB)", 1_000_000_000)
        };

        var recommendations = engine.BuildRecommendations(findings);

        var recommendation = Assert.Single(recommendations);
        Assert.Equal(@"C:\data\a.bin", recommendation.TargetPath);
        Assert.Equal(1_000_000_000, recommendation.ReclaimableBytes);
        Assert.Equal(ConfidenceTier.Low, recommendation.ConfidenceTier);
    }

    [Fact]
    public void BuildRecommendations_MergesFindingsFromDifferentDetectors_ForTheSameTarget()
    {
        var engine = new RecommendationEngine();
        var findings = new[]
        {
            new Finding(@"C:\data\a.bin", "LargeFileDetector", "Large file", 500),
            new Finding(@"C:\data\a.bin", "StaleFileDetector", "Not accessed in 2 years", 500)
        };

        var recommendations = engine.BuildRecommendations(findings);

        var recommendation = Assert.Single(recommendations);
        Assert.Equal(2, recommendation.SourceFindings.Count);
        Assert.Equal(ConfidenceTier.Medium, recommendation.ConfidenceTier);
        Assert.Equal(["Large file", "Not accessed in 2 years"], recommendation.Reasons);
    }

    [Fact]
    public void BuildRecommendations_UsesMaxSize_NotSum_WhenMergingSameTarget()
    {
        // Both findings describe the same file, so its reclaimable size must not be double-counted.
        var engine = new RecommendationEngine();
        var findings = new[]
        {
            new Finding(@"C:\data\a.bin", "LargeFileDetector", "Large file", 1000),
            new Finding(@"C:\data\a.bin", "StaleFileDetector", "Stale", 1000)
        };

        var recommendation = engine.BuildRecommendations(findings).Single();

        Assert.Equal(1000, recommendation.ReclaimableBytes);
    }

    [Fact]
    public void BuildRecommendations_OrdersByReclaimableBytesDescending()
    {
        var engine = new RecommendationEngine();
        var findings = new[]
        {
            new Finding(@"C:\data\small.bin", "LargeFileDetector", "small", 100),
            new Finding(@"C:\data\big.bin", "LargeFileDetector", "big", 10_000)
        };

        var recommendations = engine.BuildRecommendations(findings);

        Assert.Equal(@"C:\data\big.bin", recommendations[0].TargetPath);
        Assert.Equal(@"C:\data\small.bin", recommendations[1].TargetPath);
    }
}
