using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FolderPatternRules;

/// <summary>
/// Matches when the folder's own name (last path segment) is one of a configured set — used for
/// structural folders like node_modules or .git whose identity doesn't depend on their contents.
/// </summary>
public abstract class FolderNameFolderPatternRule : IFolderPatternRule
{
    private readonly HashSet<string> _folderNames;
    private readonly FolderType _folderType;
    private readonly string _ruleName;

    protected FolderNameFolderPatternRule(FolderType folderType, string ruleName, IEnumerable<string> folderNames)
    {
        _folderType = folderType;
        _ruleName = ruleName;
        _folderNames = new HashSet<string>(folderNames, StringComparer.OrdinalIgnoreCase);
    }

    public DetectedFolder? Match(string folderPath, IReadOnlyList<FileRecord> filesInFolder)
    {
        var folderName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (!_folderNames.Contains(folderName))
        {
            return null;
        }

        return new DetectedFolder(folderPath, _folderType, _ruleName, 0, 0, null, null);
    }
}
