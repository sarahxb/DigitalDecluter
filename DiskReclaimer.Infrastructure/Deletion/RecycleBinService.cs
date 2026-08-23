using DiskReclaimer.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;

namespace DiskReclaimer.Infrastructure.Deletion;

/// <summary>
/// The only place in the app that actually removes anything from disk — and even here, "remove"
/// means the Windows Recycle Bin, never a permanent delete. Every caller is expected to have already
/// gotten explicit user confirmation before invoking this.
/// </summary>
public sealed class RecycleBinService : IRecycleBinService
{
    private readonly ILogger<RecycleBinService> _logger;

    public RecycleBinService(ILogger<RecycleBinService> logger)
    {
        _logger = logger;
    }

    public Task<bool> DeleteAsync(string path, CancellationToken cancellationToken) =>
        Task.Run(() => Delete(path), cancellationToken);

    private bool Delete(string path)
    {
        if (File.Exists(path))
        {
            FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            return true;
        }

        if (Directory.Exists(path))
        {
            FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            return true;
        }

        _logger.LogWarning("Cannot delete {Path} - it no longer exists", path);
        return false;
    }
}
