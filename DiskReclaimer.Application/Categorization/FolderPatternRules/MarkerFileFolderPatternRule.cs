using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FolderPatternRules;

/// <summary>
/// Matches when the folder directly contains a file whose name matches one of a configured set of
/// markers (exact name or "*.ext" glob) — used for project roots like a .csproj or Dockerfile.
/// </summary>
public abstract class MarkerFileFolderPatternRule : IFolderPatternRule
{
    private readonly IReadOnlyList<string> _exactNames;
    private readonly IReadOnlyList<string> _extensions;
    private readonly FolderType _folderType;
    private readonly string _ruleName;

    protected MarkerFileFolderPatternRule(FolderType folderType, string ruleName, IEnumerable<string> markers)
    {
        _folderType = folderType;
        _ruleName = ruleName;

        var exact = new List<string>();
        var extensions = new List<string>();
        foreach (var marker in markers)
        {
            if (marker.StartsWith("*.", StringComparison.Ordinal))
            {
                extensions.Add(marker[1..]);
            }
            else
            {
                exact.Add(marker);
            }
        }

        _exactNames = exact;
        _extensions = extensions;
    }

    public DetectedFolder? Match(string folderPath, IReadOnlyList<FileRecord> filesInFolder)
    {
        foreach (var file in filesInFolder)
        {
            var isExactMatch = _exactNames.Contains(file.Name, StringComparer.OrdinalIgnoreCase);
            var isExtensionMatch = _extensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase);
            if (isExactMatch || isExtensionMatch)
            {
                return new DetectedFolder(folderPath, _folderType, _ruleName, 0, 0, null, null);
            }
        }

        return null;
    }
}
