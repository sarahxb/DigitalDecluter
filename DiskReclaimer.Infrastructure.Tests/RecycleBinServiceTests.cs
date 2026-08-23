using DiskReclaimer.Infrastructure.Deletion;
using Microsoft.Extensions.Logging.Abstractions;

namespace DiskReclaimer.Infrastructure.Tests;

public sealed class RecycleBinServiceTests
{
    // Only the "nothing to delete" branch is covered here — actually exercising the success path would
    // send a real item to the Recycle Bin on whatever machine runs the tests, on every test run,
    // forever. That's verified manually instead of as part of the automated suite.

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenPathDoesNotExistAsFileOrDirectory()
    {
        var service = new RecycleBinService(NullLogger<RecycleBinService>.Instance);
        var missingPath = Path.Combine(Path.GetTempPath(), "DiskReclaimerTests_missing_" + Guid.NewGuid());

        var result = await service.DeleteAsync(missingPath, CancellationToken.None);

        Assert.False(result);
    }
}
