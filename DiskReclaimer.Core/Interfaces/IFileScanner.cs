using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Core.Interfaces;

public interface IFileScanner
{
    Task<IReadOnlyList<FileRecord>> ScanAsync(string rootPath, CancellationToken cancellationToken);
}
