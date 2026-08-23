using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Core.Interfaces;

public interface IFileTypeRule
{
    bool TryCategorize(FileRecord file, out Category category);
}
