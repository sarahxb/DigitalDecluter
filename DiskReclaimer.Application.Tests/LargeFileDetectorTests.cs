using DiskReclaimer.Application.Detectors;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Tests;

public sealed class LargeFileDetectorTests
{
    private static CategorizedFile MakeFile(string path, long sizeBytes)
    {
        var now = DateTimeOffset.UtcNow;
        var record = new FileRecord(path, Path.GetFileName(path), Path.GetExtension(path), sizeBytes, now, now, now);
        return new CategorizedFile(record, Category.Unknown, null);
    }

    [Fact]
    public void Detect_FlagsFilesAtOrAboveThreshold()
    {
        var detector = new LargeFileDetector(thresholdBytes: 1000);
        var files = new[]
        {
            MakeFile(@"C:\data\huge.bin", 1000),
            MakeFile(@"C:\data\small.txt", 999)
        };

        var findings = detector.Detect(files, []).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(@"C:\data\huge.bin", finding.TargetPath);
        Assert.Equal(1000, finding.SizeBytes);
        Assert.Equal(nameof(LargeFileDetector), finding.DetectorName);
    }

    [Fact]
    public void Detect_ReturnsNothing_WhenNoFileMeetsThreshold()
    {
        var detector = new LargeFileDetector(thresholdBytes: 1_000_000);
        var files = new[] { MakeFile(@"C:\data\small.txt", 100) };

        var findings = detector.Detect(files, []).ToList();

        Assert.Empty(findings);
    }

    [Fact]
    public void Detect_UsesDefaultThreshold_WhenNoneSpecified()
    {
        var detector = new LargeFileDetector();
        var files = new[]
        {
            MakeFile(@"C:\data\just-under.bin", LargeFileDetector.DefaultThresholdBytes - 1),
            MakeFile(@"C:\data\just-over.bin", LargeFileDetector.DefaultThresholdBytes)
        };

        var findings = detector.Detect(files, []).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(@"C:\data\just-over.bin", finding.TargetPath);
    }

    [Fact]
    public void Detect_DescriptionIncludesHumanReadableSize()
    {
        var detector = new LargeFileDetector(thresholdBytes: 0);
        var files = new[] { MakeFile(@"C:\data\one-gig.bin", 1024L * 1024 * 1024) };

        var finding = detector.Detect(files, []).Single();

        Assert.Contains("GB", finding.Description);
    }
}
