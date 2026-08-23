using DiskReclaimer.Application.Exclusions;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Tests;

public sealed class ExclusionFilterTests
{
    private static CategorizedFile MakeFile(string fullPath)
    {
        var now = DateTimeOffset.UtcNow;
        var record = new FileRecord(fullPath, Path.GetFileName(fullPath), Path.GetExtension(fullPath), 1, now, now, now);
        return new CategorizedFile(record, Category.Unknown, null);
    }

    [Fact]
    public void Apply_ReturnsAllFiles_WhenNoRulesConfigured()
    {
        var result = new CategorizationResult([MakeFile(@"C:\data\a.txt")], []);

        var filtered = ExclusionFilter.Apply(result, []);

        Assert.Single(filtered.Files);
    }

    [Fact]
    public void Apply_ExcludesFilesUnderExactDirectoryPrefix()
    {
        var result = new CategorizationResult(
        [
            MakeFile(@"C:\Windows\System32\kernel.dll"),
            MakeFile(@"C:\data\keep.txt")
        ], []);
        var rules = new[] { new ExclusionRule(@"C:\Windows", "system", IsSystemFloor: true) };

        var filtered = ExclusionFilter.Apply(result, rules);

        Assert.Single(filtered.Files);
        Assert.Equal("keep.txt", filtered.Files[0].Record.Name);
    }

    [Fact]
    public void Apply_DoesNotExcludeSiblingDirectoryWithSharedPrefix()
    {
        // "C:\Windows.old" must not be treated as inside "C:\Windows" due to naive string prefixing.
        var result = new CategorizationResult([MakeFile(@"C:\Windows.old\file.txt")], []);
        var rules = new[] { new ExclusionRule(@"C:\Windows", "system", IsSystemFloor: true) };

        var filtered = ExclusionFilter.Apply(result, rules);

        Assert.Single(filtered.Files);
    }

    [Fact]
    public void Apply_SupportsWildcardGlobPatterns()
    {
        var result = new CategorizationResult(
        [
            MakeFile(@"C:\data\report.tmp"),
            MakeFile(@"C:\data\report.txt")
        ], []);
        var rules = new[] { new ExclusionRule(@"C:\data\*.tmp", "temp files", IsSystemFloor: false) };

        var filtered = ExclusionFilter.Apply(result, rules);

        Assert.Single(filtered.Files);
        Assert.Equal("report.txt", filtered.Files[0].Record.Name);
    }

    [Fact]
    public void Apply_ExcludesFoldersMatchingRules()
    {
        var folder = new DetectedFolder(@"C:\proj\node_modules", FolderType.NodeModules, "rule", 0, 0, null, null);
        var result = new CategorizationResult([], [folder]);
        var rules = new[] { new ExclusionRule(@"C:\proj\node_modules", "excluded", IsSystemFloor: false) };

        var filtered = ExclusionFilter.Apply(result, rules);

        Assert.Empty(filtered.Folders);
    }
}
