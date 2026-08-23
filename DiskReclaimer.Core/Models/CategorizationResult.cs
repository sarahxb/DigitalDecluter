namespace DiskReclaimer.Core.Models;

public sealed record CategorizationResult(
    IReadOnlyList<CategorizedFile> Files,
    IReadOnlyList<DetectedFolder> Folders);
