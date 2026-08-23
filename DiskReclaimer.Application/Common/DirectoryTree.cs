namespace DiskReclaimer.Application.Common;

/// <summary>
/// Small path-hierarchy helpers shared by anything that needs to reason about directories from a flat
/// list of file paths — the scanner only ever produces files, never directory entries.
/// </summary>
internal static class DirectoryTree
{
    /// <summary>Every unique ancestor directory of every given file path, including ones that own no file directly.</summary>
    public static HashSet<string> CollectAllAncestorDirectories(IEnumerable<string> filePaths)
    {
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var filePath in filePaths)
        {
            var directory = Path.GetDirectoryName(filePath);
            // Add returns false once we reach a directory already seen — every ancestor above it
            // must already be in the set too, so it's safe to stop walking up here.
            while (!string.IsNullOrEmpty(directory) && directories.Add(directory))
            {
                directory = Path.GetDirectoryName(directory);
            }
        }

        return directories;
    }

    /// <summary>
    /// Drops any item whose path is nested inside another item's path, keeping only the outermost
    /// ancestor of each cluster — avoids double-counting the same disk space (e.g. a node_modules
    /// folder nested inside another node_modules folder) under two separate findings.
    /// </summary>
    public static List<T> KeepOutermostOnly<T>(IEnumerable<T> items, Func<T, string> pathSelector)
    {
        var ordered = items.OrderBy(item => pathSelector(item).Length).ToList();
        var result = new List<T>();

        foreach (var candidate in ordered)
        {
            var isNestedInsideExisting = result.Any(existing => IsUnderDirectory(pathSelector(candidate), pathSelector(existing)));
            if (!isNestedInsideExisting)
            {
                result.Add(candidate);
            }
        }

        return result;
    }

    public static bool IsUnderDirectory(string path, string potentialAncestor)
    {
        var normalizedAncestor = potentialAncestor.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return path.StartsWith(normalizedAncestor + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
