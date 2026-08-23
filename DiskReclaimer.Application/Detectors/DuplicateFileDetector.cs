using System.Collections.Concurrent;
using System.IO.Hashing;
using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Detectors;

/// <summary>
/// Finds files with byte-identical content via a three-stage pipeline: group by size (cheap, cuts the
/// candidate pool drastically), then by content hash within each size group, then confirm every
/// hash match with a byte-for-byte comparison in case of a hash collision. Unlike other detectors,
/// this one has to read file content off disk, not just the metadata the scanner already collected —
/// hashing within a size group happens in parallel since it's the expensive, embarrassingly parallel
/// part of the pipeline.
/// </summary>
public sealed class DuplicateFileDetector : IRecommendationDetector
{
    private readonly long _minSizeBytes;

    public DuplicateFileDetector(long minSizeBytes = 1)
    {
        _minSizeBytes = minSizeBytes;
    }

    public IEnumerable<Finding> Detect(IReadOnlyList<CategorizedFile> files, IReadOnlyList<DetectedFolder> folders)
    {
        var sizeGroups = files
            .Where(f => f.Record.SizeBytes >= _minSizeBytes)
            .GroupBy(f => f.Record.SizeBytes)
            .Where(g => g.Count() > 1);

        foreach (var sizeGroup in sizeGroups)
        {
            var hashGroups = ComputeHashesInParallel(sizeGroup)
                .GroupBy(x => x.Hash)
                .Where(g => g.Count() > 1);

            foreach (var hashGroup in hashGroups)
            {
                var candidates = hashGroup.Select(x => x.File).ToList();

                foreach (var confirmedGroup in ConfirmByteForByte(candidates))
                {
                    var groupId = Guid.NewGuid();
                    var allPaths = confirmedGroup.Select(f => f.Record.FullPath).ToList();

                    foreach (var file in confirmedGroup)
                    {
                        var otherPaths = allPaths.Where(p => p != file.Record.FullPath).ToList();
                        yield return new DuplicateFinding(
                            file.Record.FullPath,
                            nameof(DuplicateFileDetector),
                            $"Duplicate of {otherPaths.Count} other file(s)",
                            file.Record.SizeBytes,
                            groupId,
                            otherPaths);
                    }
                }
            }
        }
    }

    private static List<(CategorizedFile File, ulong Hash)> ComputeHashesInParallel(IEnumerable<CategorizedFile> candidates)
    {
        var hashed = new ConcurrentBag<(CategorizedFile, ulong)>();

        Parallel.ForEach(candidates, candidate =>
        {
            var hash = TryComputeHash(candidate.Record.FullPath);
            if (hash is not null)
            {
                hashed.Add((candidate, hash.Value));
            }
        });

        return hashed.ToList();
    }

    /// <summary>Partitions same-size, same-hash candidates into clusters of genuinely identical content.</summary>
    private static IEnumerable<List<CategorizedFile>> ConfirmByteForByte(List<CategorizedFile> candidates)
    {
        var clusters = new List<List<CategorizedFile>>();

        foreach (var candidate in candidates)
        {
            var matchedCluster = clusters.FirstOrDefault(cluster =>
                AreByteIdentical(cluster[0].Record.FullPath, candidate.Record.FullPath));

            if (matchedCluster is not null)
            {
                matchedCluster.Add(candidate);
            }
            else
            {
                clusters.Add([candidate]);
            }
        }

        return clusters.Where(c => c.Count > 1);
    }

    private static ulong? TryComputeHash(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var hasher = new XxHash64();
            hasher.Append(stream);
            return BitConverter.ToUInt64(hasher.GetCurrentHash());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool AreByteIdentical(string pathA, string pathB)
    {
        const int bufferSize = 81920;

        try
        {
            using var streamA = File.OpenRead(pathA);
            using var streamB = File.OpenRead(pathB);

            if (streamA.Length != streamB.Length)
            {
                return false;
            }

            var bufferA = new byte[bufferSize];
            var bufferB = new byte[bufferSize];

            int bytesRead;
            while ((bytesRead = streamA.Read(bufferA, 0, bufferSize)) > 0)
            {
                var bytesReadB = streamB.Read(bufferB, 0, bytesRead);
                if (bytesReadB != bytesRead || !bufferA.AsSpan(0, bytesRead).SequenceEqual(bufferB.AsSpan(0, bytesRead)))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
