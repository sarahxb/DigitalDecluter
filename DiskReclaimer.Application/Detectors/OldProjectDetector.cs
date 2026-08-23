using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Detectors;

/// <summary>
/// Flags project-root folders (Git repos, Visual Studio/IntelliJ projects, Docker contexts) that have
/// had no activity anywhere in their whole tree for a long time — old coursework, abandoned side
/// projects. Uses the folder's NewestModifiedUtc — the most recently touched file anywhere under it,
/// already aggregated by Categorizer — as the recency signal: any file touched recently means someone
/// is still using the project, even if most of the tree is untouched. Dependency/build folders
/// (node_modules, .venv, bin/obj) are deliberately excluded — they aren't projects themselves, and are
/// usually nested inside one of the types above anyway.
/// </summary>
public sealed class OldProjectDetector : IRecommendationDetector
{
    public static readonly TimeSpan DefaultStaleThreshold = TimeSpan.FromDays(365);

    private static readonly FolderType[] ProjectFolderTypes =
    [
        FolderType.GitRepo,
        FolderType.VisualStudioProject,
        FolderType.IntelliJProject,
        FolderType.DockerContext
    ];

    private readonly TimeSpan _staleThreshold;
    private readonly Func<DateTimeOffset> _nowProvider;

    public OldProjectDetector(TimeSpan? staleThreshold = null, Func<DateTimeOffset>? nowProvider = null)
    {
        _staleThreshold = staleThreshold ?? DefaultStaleThreshold;
        _nowProvider = nowProvider ?? (() => DateTimeOffset.UtcNow);
    }

    public IEnumerable<Finding> Detect(IReadOnlyList<CategorizedFile> files, IReadOnlyList<DetectedFolder> folders)
    {
        var now = _nowProvider();

        foreach (var folder in folders)
        {
            if (!ProjectFolderTypes.Contains(folder.FolderType) || folder.NewestModifiedUtc is null)
            {
                continue;
            }

            var age = now - folder.NewestModifiedUtc.Value;
            if (age < _staleThreshold)
            {
                continue;
            }

            yield return new Finding(
                folder.Path,
                nameof(OldProjectDetector),
                $"Old {DescribeFolderType(folder.FolderType)}, no activity in {(int)age.TotalDays} days ({folder.FileCount} file(s))",
                folder.AggregateSizeBytes);
        }
    }

    private static string DescribeFolderType(FolderType folderType) => folderType switch
    {
        FolderType.GitRepo => "Git repository",
        FolderType.VisualStudioProject => "Visual Studio project",
        FolderType.IntelliJProject => "IntelliJ project",
        FolderType.DockerContext => "Docker project",
        _ => "project"
    };
}
