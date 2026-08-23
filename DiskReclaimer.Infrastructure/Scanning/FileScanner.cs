using System.Security;
using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;
using Microsoft.Extensions.Logging;

namespace DiskReclaimer.Infrastructure.Scanning;

/// <summary>
/// Walks the filesystem from a root path and produces a flat list of FileRecords.
/// Resilient to inaccessible directories, missing files, and reparse points (skipped to avoid symlink cycles).
/// </summary>
public sealed class FileScanner : IFileScanner
{
    private readonly ILogger<FileScanner> _logger;

    public FileScanner(ILogger<FileScanner> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<FileRecord>> ScanAsync(string rootPath, CancellationToken cancellationToken)
    {
        return Task.Run(() => Scan(rootPath, cancellationToken), cancellationToken);
    }

    private IReadOnlyList<FileRecord> Scan(string rootPath, CancellationToken cancellationToken)
    {
        var results = new List<FileRecord>();
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(rootPath);

        while (pendingDirectories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentDirectory = pendingDirectories.Pop();

            IEnumerable<string> subDirectories;
            try
            {
                subDirectories = Directory.EnumerateDirectories(currentDirectory);
            }
            catch (Exception ex) when (IsAccessException(ex))
            {
                _logger.LogDebug(ex, "Skipping inaccessible directory {Directory}", currentDirectory);
                continue;
            }

            foreach (var subDirectory in subDirectories)
            {
                if (IsReparsePoint(subDirectory))
                {
                    continue;
                }

                pendingDirectories.Push(subDirectory);
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(currentDirectory);
            }
            catch (Exception ex) when (IsAccessException(ex))
            {
                _logger.LogDebug(ex, "Skipping inaccessible directory {Directory}", currentDirectory);
                continue;
            }

            foreach (var filePath in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var record = TryCreateFileRecord(filePath);
                if (record is not null)
                {
                    results.Add(record);
                }
            }
        }

        return results;
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
