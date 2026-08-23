using DiskReclaimer.Application.Detectors;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Tests;

public sealed class StaleFileDetectorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 0, 0, 0, TimeSpan.Zero);

    private static CategorizedFile MakeFile(string name, DateTimeOffset lastModifiedUtc, DateTimeOffset lastAccessedUtc)
    {
        var record = new FileRecord(
            @"C:\data\" + name, name, Path.GetExtension(name), 100,
            CreatedUtc: lastModifiedUtc, LastModifiedUtc: lastModifiedUtc, LastAccessedUtc: lastAccessedUtc);
        return new CategorizedFile(record, Category.Unknown, null);
    }

    [Fact]
    public void Detect_FlagsFileOlderThanThreshold()
    {
        var oldFile = MakeFile("old.txt", Now.AddDays(-200), Now.AddDays(-200));
        var detector = new StaleFileDetector(TimeSpan.FromDays(180), () => Now);

        var findings = detector.Detect([oldFile], []).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(oldFile.Record.FullPath, finding.TargetPath);
        Assert.Equal(nameof(StaleFileDetector), finding.DetectorName);
    }

    [Fact]
    public void Detect_DoesNotFlagRecentFile()
    {
        var recentFile = MakeFile("recent.txt", Now.AddDays(-10), Now.AddDays(-10));
        var detector = new StaleFileDetector(TimeSpan.FromDays(180), () => Now);

        var findings = detector.Detect([recentFile], []).ToList();

        Assert.Empty(findings);
    }

    [Fact]
    public void Detect_UsesMoreRecentAccessTime_WhenModifiedIsOldButRecentlyOpened()
    {
        var file = MakeFile("opened.txt", lastModifiedUtc: Now.AddDays(-400), lastAccessedUtc: Now.AddDays(-5));
        var detector = new StaleFileDetector(TimeSpan.FromDays(180), () => Now);

        var findings = detector.Detect([file], []).ToList();

        Assert.Empty(findings);
    }

    [Fact]
    public void Detect_UsesMoreRecentModifiedTime_WhenAccessTimeIsUnreliablyOld()
    {
        // Simulates NTFS with last-access tracking disabled: access time frozen at creation, far older
        // than the file's actual last edit.
        var file = MakeFile("edited.txt", lastModifiedUtc: Now.AddDays(-10), lastAccessedUtc: Now.AddDays(-400));
        var detector = new StaleFileDetector(TimeSpan.FromDays(180), () => Now);

        var findings = detector.Detect([file], []).ToList();

        Assert.Empty(findings);
    }

    [Fact]
    public void Detect_UsesDefaultThreshold_WhenNoneSpecified()
    {
        var justUnder = MakeFile("just-under.txt",
            Now.Add(-StaleFileDetector.DefaultStaleThreshold).AddDays(1),
            Now.Add(-StaleFileDetector.DefaultStaleThreshold).AddDays(1));
        var justOver = MakeFile("just-over.txt",
            Now.Add(-StaleFileDetector.DefaultStaleThreshold).AddDays(-1),
            Now.Add(-StaleFileDetector.DefaultStaleThreshold).AddDays(-1));
        var detector = new StaleFileDetector(nowProvider: () => Now);

        var findings = detector.Detect([justUnder, justOver], []).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(justOver.Record.FullPath, finding.TargetPath);
    }
}
