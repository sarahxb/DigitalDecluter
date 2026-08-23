using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FileTypeRules;

/// <summary>Matches by well-known file name rather than extension (e.g. Thumbs.db has no useful extension).</summary>
public sealed class SystemJunkFileTypeRule : IFileTypeRule
{
    private static readonly HashSet<string> JunkFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Thumbs.db", "desktop.ini", ".DS_Store", "ehthumbs.db"
    };

    public bool TryCategorize(FileRecord file, out Category category)
    {
        if (JunkFileNames.Contains(file.Name))
        {
            category = Category.SystemJunk;
            return true;
        }

        category = default;
        return false;
    }
}
