namespace DiskReclaimer.Core.Interfaces;

public interface IRecycleBinService
{
    /// <summary>
    /// Sends a file or folder to the Recycle Bin. Returns false if the path no longer exists (nothing
    /// to delete); throws if the delete itself fails (in use, permission denied, ...).
    /// </summary>
    Task<bool> DeleteAsync(string path, CancellationToken cancellationToken);
}
