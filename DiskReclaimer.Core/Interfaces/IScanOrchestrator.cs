using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Core.Interfaces;

public interface IScanOrchestrator
{
    Task<ScanResult> ScanAsync(string rootPath, IProgress<ScanProgress>? progress, CancellationToken cancellationToken);
}
