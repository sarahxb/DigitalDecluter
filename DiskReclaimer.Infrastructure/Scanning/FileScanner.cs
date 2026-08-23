using System.Collections.Concurrent;
using System.Security;
using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;
using Microsoft.Extensions.Logging;

namespace DiskReclaimer.Infrastructure.Scanning;

/// <summary>
/// Walks the filesystem from a root path and produces a flat list of FileRecords, fanning directory
/// traversal out across a small worker pool — on a large drive, syscall latency per directory
/// dominates, so overlapping many of them concurrently matters far more than raw CPU.
/// Resilient to inaccessible directories, missing files, and reparse points (skipped to avoid symlink cycles).
/// </summary>
public sealed class FileScanner : IFileScanner
{
    private const int DefaultDegreeOfParallelism = 8;
    private const int ProgressReportInterval = 500;

    private readonly ILogger<FileScanner> _logger;
    private readonly int _degreeOfParallelism;

    public FileScanner(ILogger<FileScanner> logger, int degreeOfParallelism = DefaultDegreeOfParallelism)
    {
        _logger = logger;
        _degreeOfParallelism = degreeOfParallelism;
    }

    public async Task<IReadOnlyList<FileRecord>> ScanAsync(
        string rootPath, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        var results = new ConcurrentBag<FileRecord>();
        using var pendingDirectories = new BlockingCollection<string>();

        // Outstanding units of work: the root, plus every directory discovered but not yet processed.
        // Reaches zero exactly when nothing is left to do anywhere, at which point the collection is
        // closed so every worker's GetConsumingEnumerable() loop ends. See ProcessDirectory for why
        // this can't race: a directory's own decrement always happens after any increments for the
        // children it discovered, so the count can never hit zero while unprocessed work still exists.
        var pendingCount = 1;
        var filesScanned = 0;

        pendingDirectories.Add(rootPath, cancellationToken);

        void ProcessDirectory(string directory)
        {
            try
            {
                IEnumerable<string> subDirectories;
                try
                {
                    subDirectories = Directory.EnumerateDirectories(directory);
                }
                catch (Exception ex) when (IsAccessException(ex))
                {
                    _logger.LogDebug(ex, "Skipping inaccessible directory {Directory}", directory);
                    subDirectories = [];
                }

                var newDirectories = subDirectories.Where(d => !IsReparsePoint(d)).ToList();
                if (newDirectories.Count > 0)
                {
                    Interlocked.Add(ref pendingCount, newDirectories.Count);
                    foreach (var newDirectory in newDirectories)
                    {
                        pendingDirectories.Add(newDirectory, cancellationToken);
                    }
                }

                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(directory);
                }
                catch (Exception ex) when (IsAccessException(ex))
                {
                    _logger.LogDebug(ex, "Skipping inaccessible directory {Directory}", directory);
                    return;
                }

                foreach (var filePath in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var record = TryCreateFileRecord(filePath);
                    if (record is null)
                    {
                        continue;
                    }

                    results.Add(record);
                    var scannedSoFar = Interlocked.Increment(ref filesScanned);
                    if (scannedSoFar % ProgressReportInterval == 0)
                    {
                        progress?.Report(new ScanProgress(scannedSoFar, directory));
                    }
                }
            }
            finally
            {
                if (Interlocked.Decrement(ref pendingCount) == 0)
                {
                    pendingDirectories.CompleteAdding();
                }
            }
        }

        var workers = Enumerable.Range(0, _degreeOfParallelism)
            .Select(_ => Task.Run(() =>
            {
                foreach (var directory in pendingDirectories.GetConsumingEnumerable(cancellationToken))
                {
                    ProcessDirectory(directory);
                }
            }, cancellationToken))
            .ToArray();

        await Task.WhenAll(workers);

        progress?.Report(new ScanProgress(filesScanned, null));

        return results.ToList();
    }

    private FileRecord? TryCreateFileRecord(string filePath)
    {
        try
        {
            var info = new FileInfo(filePath);
            if (!info.Exists || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                return null;
            }

            return new FileRecord(
                FullPath: info.FullName,
                Name: info.Name,
                Extension: info.Extension,
                SizeBytes: info.Length,
                CreatedUtc: info.CreationTimeUtc,
                LastModifiedUtc: info.LastWriteTimeUtc,
                LastAccessedUtc: info.LastAccessTimeUtc);
        }
        catch (Exception ex) when (IsAccessException(ex))
        {
            _logger.LogDebug(ex, "Skipping inaccessible file {File}", filePath);
            return null;
        }
    }

    private static bool IsReparsePoint(string directoryPath)
    {
        try
        {
            return new DirectoryInfo(directoryPath).Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        catch (Exception ex) when (IsAccessException(ex))
        {
            return true;
        }
    }

    private static bool IsAccessException(Exception ex) =>
        ex is UnauthorizedAccessException or IOException or SecurityException;
}
