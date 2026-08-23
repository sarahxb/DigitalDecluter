using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Core.Interfaces;

public interface ICategorizer
{
    CategorizationResult Categorize(IReadOnlyList<FileRecord> files);
}
