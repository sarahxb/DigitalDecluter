namespace DiskReclaimer.Core.Models;

public sealed record ScanResult(
    CategorizationResult Categorization,
    IReadOnlyList<Recommendation> Recommendations,
    IReadOnlyList<InsightSummary> Insights);
