namespace DiskReclaimer.Core.Models;

public sealed record ExclusionRule(
    string PathPattern,
    string Reason,
    bool IsSystemFloor);
