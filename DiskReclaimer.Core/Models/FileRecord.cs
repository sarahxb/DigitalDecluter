namespace DiskReclaimer.Core.Models;

public sealed record FileRecord(
    string FullPath,
    string Name,
    string Extension,
    long SizeBytes,
    DateTimeOffset CreatedUtc,
    DateTimeOffset LastModifiedUtc,
    DateTimeOffset LastAccessedUtc);
