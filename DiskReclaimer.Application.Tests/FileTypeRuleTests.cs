using DiskReclaimer.Application.Categorization.FileTypeRules;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Tests;

public sealed class FileTypeRuleTests
{
    private static FileRecord MakeFile(string name)
    {
        var now = DateTimeOffset.UtcNow;
        return new FileRecord(@"C:\dir\" + name, name, Path.GetExtension(name), 1, now, now, now);
    }

    [Theory]
    [InlineData("Thumbs.db")]
    [InlineData("desktop.ini")]
    [InlineData(".DS_Store")]
    public void SystemJunkFileTypeRule_MatchesKnownJunkFileNames(string name)
    {
        var rule = new SystemJunkFileTypeRule();

        var matched = rule.TryCategorize(MakeFile(name), out var category);

        Assert.True(matched);
        Assert.Equal(Category.SystemJunk, category);
    }

    [Fact]
    public void SystemJunkFileTypeRule_DoesNotMatchUnrelatedFile()
    {
        var rule = new SystemJunkFileTypeRule();

        var matched = rule.TryCategorize(MakeFile("notes.txt"), out _);

        Assert.False(matched);
    }

    [Theory]
    [InlineData(".exe")]
    [InlineData(".msi")]
    public void InstallerFileTypeRule_MatchesInstallerExtensions(string extension)
    {
        var rule = new InstallerFileTypeRule();

        var matched = rule.TryCategorize(MakeFile("setup" + extension), out var category);

        Assert.True(matched);
        Assert.Equal(Category.Installer, category);
    }

    [Fact]
    public void InstallerFileTypeRule_IsCaseInsensitive()
    {
        var rule = new InstallerFileTypeRule();

        var matched = rule.TryCategorize(MakeFile("SETUP.EXE"), out var category);

        Assert.True(matched);
        Assert.Equal(Category.Installer, category);
    }

    [Fact]
    public void ArchiveFileTypeRule_MatchesIsoAsArchive()
    {
        var rule = new ArchiveFileTypeRule();

        var matched = rule.TryCategorize(MakeFile("linux.iso"), out var category);

        Assert.True(matched);
        Assert.Equal(Category.Archive, category);
    }
}
