namespace DiskReclaimer.Core.Models;

public enum ConfidenceTier
{
    Low,
    Medium,
    High
}

public sealed record Recommendation(
    string TargetPath,
    ConfidenceTier ConfidenceTier,
    long ReclaimableBytes,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<Finding> SourceFindings);
