using DiskReclaimer.Core.Models;
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

        var results = await scanner.ScanAsync(_root, null, CancellationToken.None);

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

        var results = await scanner.ScanAsync(_root, null, CancellationToken.None);

        Assert.Contains(results, f => f.Name == "visible.txt");
        Assert.Contains(results, f => f.Name == "inside.txt");
        Assert.Single(results, f => f.Name == "inside.txt");
    }

    [Fact]
    public async Task ScanAsync_ReturnsEmptyList_WhenRootHasNoFiles()
    {
        var scanner = new FileScanner(NullLogger<FileScanner>.Instance);

        var results = await scanner.ScanAsync(_root, null, CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ScanAsync_ReportsFinalProgress_MatchingTotalFilesFound()
    {
        File.WriteAllText(Path.Combine(_root, "a.txt"), "a");
        File.WriteAllText(Path.Combine(_root, "b.txt"), "b");
        File.WriteAllText(Path.Combine(_root, "c.txt"), "c");

        var reports = new List<ScanProgress>();
        var progress = new Progress<ScanProgress>(reports.Add);
        var scanner = new FileScanner(NullLogger<FileScanner>.Instance);

        var results = await scanner.ScanAsync(_root, progress, CancellationToken.None);

        // Progress<T> marshals via SynchronizationContext.Post, which — with no context installed in a
        // unit test — runs synchronously on ThreadPool threads, so all reports are visible by the time
        // ScanAsync's returned task completes.
        Assert.NotEmpty(reports);
        Assert.Equal(results.Count, reports[^1].FilesScanned);
    }

    [Fact]
    public async Task ScanAsync_FindsEveryFile_AcrossManyConcurrentlyProcessedDirectories()
    {
        const int subdirectoryCount = 20;
        const int filesPerSubdirectory = 5;

        for (var i = 0; i < subdirectoryCount; i++)
        {
            var subdirectory = Path.Combine(_root, $"dir{i}");
            Directory.CreateDirectory(subdirectory);
            for (var j = 0; j < filesPerSubdirectory; j++)
            {
                File.WriteAllText(Path.Combine(subdirectory, $"file{j}.txt"), "x");
            }
        }

        var scanner = new FileScanner(NullLogger<FileScanner>.Instance, degreeOfParallelism: 8);

        var results = await scanner.ScanAsync(_root, null, CancellationToken.None);

        Assert.Equal(subdirectoryCount * filesPerSubdirectory, results.Count);
        Assert.Equal(results.Count, results.Select(f => f.FullPath).Distinct().Count());
    }
}
