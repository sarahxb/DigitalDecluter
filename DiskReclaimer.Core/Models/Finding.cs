namespace DiskReclaimer.Core.Models;

/// <summary>
/// Raw, unscored observation from a single detector: "this target could reclaim SizeBytes of space,
/// for this reason." Carries no confidence or priority — only RecommendationEngine assigns those,
/// once it can see every detector's findings for a target together.
/// </summary>
public record Finding(
    string TargetPath,
    string DetectorName,
    string Description,
    long SizeBytes);

public sealed record DuplicateFinding(
    string TargetPath,
    string DetectorName,
    string Description,
    long SizeBytes,
    Guid DuplicateGroupId,
    IReadOnlyList<string> OtherPaths)
    : Finding(TargetPath, DetectorName, Description, SizeBytes);
