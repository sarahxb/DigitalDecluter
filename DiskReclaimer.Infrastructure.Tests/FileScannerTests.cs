using DiskReclaimer.Infrastructure.Scanning;
using Microsoft.Extensions.Logging.Abstractions;

namespace DiskReclaimer.Infrastructure.Tests;

public sealed class FileScannerTests : IDisposable
{
    private readonly string _root;

    public FileScannerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "DiskReclaimerTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task ScanAsync_FindsFilesInRootAndNestedDirectories()
    {
        File.WriteAllText(Path.Combine(_root, "a.txt"), "a");
        var nested = Path.Combine(_root, "nested");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(nested, "b.txt"), "bb");

        var scanner = new FileScanner(NullLogger<FileScanner>.Instance);

        var results = await scanner.ScanAsync(_root, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, f => f.Name == "a.txt" && f.SizeBytes == 1);
        Assert.Contains(results, f => f.Name == "b.txt" && f.SizeBytes == 2);
    }

    [Fact]
    public async Task ScanAsync_SkipsReparsePointDirectories_WithoutFollowingThem()
    {
        var real = Path.Combine(_root, "real");
        Directory.CreateDirectory(real);
        File.WriteAllText(Path.Combine(real, "inside.txt"), "x");
        File.WriteAllText(Path.Combine(_root, "visible.txt"), "v");

        var linkPath = Path.Combine(_root, "link");
        try
        {
            Directory.CreateSymbolicLink(linkPath, real);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // Creating symlinks can require elevated privileges on some machines; skip if unavailable.
            return;
        }

        var scanner = new FileScanner(NullLogger<FileScanner>.Instance);

        var results = await scanner.ScanAsync(_root, CancellationToken.None);

        Assert.Contains(results, f => f.Name == "visible.txt");
        Assert.Contains(results, f => f.Name == "inside.txt");
        Assert.Single(results, f => f.Name == "inside.txt");
    }

    [Fact]
    public async Task ScanAsync_ReturnsEmptyList_WhenRootHasNoFiles()
    {
        var scanner = new FileScanner(NullLogger<FileScanner>.Instance);

        var results = await scanner.ScanAsync(_root, CancellationToken.None);

        Assert.Empty(results);
    }
}
