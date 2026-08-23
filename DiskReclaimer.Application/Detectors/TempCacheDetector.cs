using DiskReclaimer.Application.Common;
using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Detectors;

/// <summary>
/// Detects well-known junk folders — the user's and Windows' temp folders, Downloads, and common
/// cache directory names (browser caches, __pycache__, etc.) — and reports one Finding per folder root
/// covering everything under it, rather than flagging each file individually.
/// </summary>
public sealed class TempCacheDetector : IRecommendationDetector
{
    private readonly IReadOnlyList<(string Reason, Func<string, bool> IsMatch)> _patterns = BuildDefaultPatterns();

    public IEnumerable<Finding> Detect(IReadOnlyList<CategorizedFile> files, IReadOnlyList<DetectedFolder> folders)
    {
        var allDirectories = DirectoryTree.CollectAllAncestorDirectories(files.Select(f => f.Record.FullPath));

        var matches = new List<(string Path, string Reason)>();
        foreach (var directory in allDirectories)
        {
            foreach (var (reason, isMatch) in _patterns)
            {
                if (isMatch(directory))
                {
                    matches.Add((directory, reason));
                    break;
                }
            }
        }

        var outermostMatches = DirectoryTree.KeepOutermostOnly(matches, m => m.Path);

        foreach (var (folderPath, reason) in outermostMatches)
        {
            var prefix = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var descendantFiles = files.Where(f => f.Record.FullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            var totalSize = descendantFiles.Sum(f => f.Record.SizeBytes);

            yield return new Finding(
                folderPath,
                nameof(TempCacheDetector),
                $"{reason} ({descendantFiles.Count} file(s))",
                totalSize);
        }
    }

    private static List<(string Reason, Func<string, bool> IsMatch)> BuildDefaultPatterns()
    {
        var userTemp = NormalizePath(Path.GetTempPath());
        var windowsTemp = NormalizePath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"));
        var downloads = NormalizePath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));

        string[] tempFolderNames = ["Temp", "tmp"];
        string[] cacheFolderNames = ["Cache", "Caches", "Cache2", "GPUCache", "Code Cache", "__pycache__"];

        return
        [
            ("User temp folder", p => PathEquals(p, userTemp)),
            ("Windows temp folder", p => PathEquals(p, windowsTemp)),
            ("Downloads folder", p => PathEquals(p, downloads)),
            ("Temp folder", p => tempFolderNames.Contains(Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)),
            ("Cache folder", p => cacheFolderNames.Contains(Path.GetFileName(p), StringComparer.OrdinalIgnoreCase))
        ];
    }

    private static string NormalizePath(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static bool PathEquals(string a, string b) =>
        NormalizePath(a).Equals(b, StringComparison.OrdinalIgnoreCase);
}
