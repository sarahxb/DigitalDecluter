using DiskReclaimer.Application.Common;
using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization;

/// <summary>
/// Pure, I/O-free enrichment of the flat FileRecord list the scanner produced: assigns each file a
/// Category via the first matching IFileTypeRule, and detects folder-level patterns (project roots,
/// node_modules, etc.) via the first matching IFolderPatternRule applied to each directory's own files.
/// </summary>
public sealed class Categorizer : ICategorizer
{
    private readonly IReadOnlyList<IFileTypeRule> _fileTypeRules;
    private readonly IReadOnlyList<IFolderPatternRule> _folderPatternRules;

    public Categorizer(IReadOnlyList<IFileTypeRule> fileTypeRules, IReadOnlyList<IFolderPatternRule> folderPatternRules)
    {
        _fileTypeRules = fileTypeRules;
        _folderPatternRules = folderPatternRules;
    }

    public CategorizationResult Categorize(IReadOnlyList<FileRecord> files)
    {
        var categorizedFiles = new List<CategorizedFile>(files.Count);
        foreach (var file in files)
        {
            categorizedFiles.Add(CategorizeFile(file));
        }

        var filesByDirectory = files
            .GroupBy(f => Path.GetDirectoryName(f.FullPath) ?? string.Empty)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<FileRecord>)g.ToList());

        // Folder-pattern rules need to run over every ancestor directory, not just ones that directly
        // own a file — a node_modules folder, for example, is usually all subdirectories and holds no
        // file of its own.
        var allDirectories = DirectoryTree.CollectAllAncestorDirectories(files.Select(f => f.FullPath));

        var detectedFolders = new List<DetectedFolder>();
        foreach (var directory in allDirectories)
        {
            var filesInDirectory = filesByDirectory.GetValueOrDefault(directory, []);
            var match = MatchFolder(directory, filesInDirectory);
            if (match is not null)
            {
                detectedFolders.Add(match);
            }
        }

        var topLevelFolders = DirectoryTree.KeepOutermostOnly(detectedFolders, f => f.Path);
        var aggregatedFolders = topLevelFolders
            .Select(folder => WithAggregates(folder, files))
            .ToList();

        return new CategorizationResult(categorizedFiles, aggregatedFolders);
    }

    private CategorizedFile CategorizeFile(FileRecord file)
    {
        foreach (var rule in _fileTypeRules)
        {
            if (rule.TryCategorize(file, out var category))
            {
                return new CategorizedFile(file, category, rule.GetType().Name);
            }
        }

        return new CategorizedFile(file, Category.Unknown, null);
    }

    private DetectedFolder? MatchFolder(string directory, IReadOnlyList<FileRecord> filesInDirectory)
    {
        foreach (var rule in _folderPatternRules)
        {
            var match = rule.Match(directory, filesInDirectory);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static DetectedFolder WithAggregates(DetectedFolder folder, IReadOnlyList<FileRecord> allFiles)
    {
        var prefix = folder.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        var descendantFiles = allFiles
            .Where(f => f.FullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (descendantFiles.Count == 0)
        {
            return folder;
        }

        return folder with
        {
            AggregateSizeBytes = descendantFiles.Sum(f => f.SizeBytes),
            FileCount = descendantFiles.Count,
            NewestModifiedUtc = descendantFiles.Max(f => f.LastModifiedUtc),
            OldestModifiedUtc = descendantFiles.Min(f => f.LastModifiedUtc)
        };
    }
}
