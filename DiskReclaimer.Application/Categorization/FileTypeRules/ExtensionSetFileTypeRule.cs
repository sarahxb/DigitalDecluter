using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FileTypeRules;

/// <summary>
/// Categorizes a file when its extension appears in a configured set. Reused across the built-in
/// file-type rules below, each supplying a different (Category, extension set) pair.
/// </summary>
public abstract class ExtensionSetFileTypeRule : IFileTypeRule
{
    private readonly HashSet<string> _extensions;
    private readonly Category _category;

    protected ExtensionSetFileTypeRule(Category category, IEnumerable<string> extensions)
    {
        _category = category;
        _extensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
    }

    public bool TryCategorize(FileRecord file, out Category category)
    {
        if (_extensions.Contains(file.Extension))
        {
            category = _category;
            return true;
        }

        category = default;
        return false;
    }
}
