namespace DiskReclaimer.Core.Models;

public sealed record CategorizedFile(
    FileRecord Record,
    Category Category,
    string? MatchedRule);
