using DiskReclaimer.Application.Detectors;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Tests;

public sealed class OldProjectDetectorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 0, 0, 0, TimeSpan.Zero);

    private static DetectedFolder MakeFolder(
        string path, FolderType folderType, DateTimeOffset? newestModifiedUtc, long sizeBytes = 1000, int fileCount = 5) =>
        new(path, folderType, "rule", sizeBytes, fileCount, newestModifiedUtc, newestModifiedUtc);

    [Fact]
    public void Detect_FlagsGitRepo_WithNoRecentActivity()
    {
        var folder = MakeFolder(@"C:\old-repo", FolderType.GitRepo, Now.AddDays(-500));
        var detector = new OldProjectDetector(TimeSpan.FromDays(365), () => Now);

        var findings = detector.Detect([], [folder]).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(@"C:\old-repo", finding.TargetPath);
        Assert.Equal(nameof(OldProjectDetector), finding.DetectorName);
        Assert.Equal(1000, finding.SizeBytes);
    }

    [Fact]
    public void Detect_DoesNotFlagRecentlyActiveGitRepo()
    {
        var folder = MakeFolder(@"C:\active-repo", FolderType.GitRepo, Now.AddDays(-5));
        var detector = new OldProjectDetector(TimeSpan.FromDays(365), () => Now);

        var findings = detector.Detect([], [folder]).ToList();

        Assert.Empty(findings);
    }

    [Theory]
    [InlineData(FolderType.NodeModules)]
    [InlineData(FolderType.PythonVenv)]
    [InlineData(FolderType.BuildOutput)]
    public void Detect_DoesNotFlagDependencyOrBuildFolders_EvenIfVeryOld(FolderType folderType)
    {
        var folder = MakeFolder(@"C:\proj\deps", folderType, Now.AddDays(-2000));
        var detector = new OldProjectDetector(TimeSpan.FromDays(365), () => Now);

        var findings = detector.Detect([], [folder]).ToList();

        Assert.Empty(findings);
    }

    [Theory]
    [InlineData(FolderType.VisualStudioProject)]
    [InlineData(FolderType.IntelliJProject)]
    [InlineData(FolderType.DockerContext)]
    [InlineData(FolderType.GitRepo)]
    public void Detect_FlagsAllProjectFolderTypes(FolderType folderType)
    {
        var folder = MakeFolder(@"C:\proj\old", folderType, Now.AddDays(-500));
        var detector = new OldProjectDetector(TimeSpan.FromDays(365), () => Now);

        var findings = detector.Detect([], [folder]).ToList();

        Assert.Single(findings);
    }

    [Fact]
    public void Detect_SkipsFolder_WhenNewestModifiedIsNull()
    {
        var folder = MakeFolder(@"C:\empty-detected-folder", FolderType.GitRepo, newestModifiedUtc: null);
        var detector = new OldProjectDetector(TimeSpan.FromDays(365), () => Now);

        var findings = detector.Detect([], [folder]).ToList();

        Assert.Empty(findings);
    }

    [Fact]
    public void Detect_UsesDefaultThreshold_WhenNoneSpecified()
    {
        var justUnder = MakeFolder(@"C:\just-under",
            FolderType.GitRepo, Now.Add(-OldProjectDetector.DefaultStaleThreshold).AddDays(1));
        var justOver = MakeFolder(@"C:\just-over",
            FolderType.GitRepo, Now.Add(-OldProjectDetector.DefaultStaleThreshold).AddDays(-1));
        var detector = new OldProjectDetector(nowProvider: () => Now);

        var findings = detector.Detect([], [justUnder, justOver]).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(@"C:\just-over", finding.TargetPath);
    }

    [Fact]
    public void Detect_ReportsAggregateSizeBytes_NotIndividualFileSize()
    {
        var folder = MakeFolder(@"C:\old-repo", FolderType.GitRepo, Now.AddDays(-500), sizeBytes: 123_456);
        var detector = new OldProjectDetector(TimeSpan.FromDays(365), () => Now);

        var finding = detector.Detect([], [folder]).Single();

        Assert.Equal(123_456, finding.SizeBytes);
    }
}
