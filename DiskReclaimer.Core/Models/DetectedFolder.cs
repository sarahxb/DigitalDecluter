namespace DiskReclaimer.Core.Models;

public enum FolderType
{
    GitRepo,
    NodeModules,
    PythonVenv,
    VisualStudioProject,
    IntelliJProject,
    BuildOutput,
    DockerContext
}

public sealed record DetectedFolder(
    string Path,
    FolderType FolderType,
    string? MatchedRule,
    long AggregateSizeBytes,
    int FileCount,
    DateTimeOffset? NewestModifiedUtc,
    DateTimeOffset? OldestModifiedUtc);
