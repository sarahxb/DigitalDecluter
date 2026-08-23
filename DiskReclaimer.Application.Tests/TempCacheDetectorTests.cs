using DiskReclaimer.Application.Detectors;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Tests;

public sealed class TempCacheDetectorTests
{
    private static CategorizedFile MakeFile(string fullPath, long sizeBytes = 100)
    {
        var now = DateTimeOffset.UtcNow;
        var record = new FileRecord(fullPath, Path.GetFileName(fullPath), Path.GetExtension(fullPath), sizeBytes, now, now, now);
        return new CategorizedFile(record, Category.Unknown, null);
    }

    [Fact]
    public void Detect_MatchesUserTempFolder()
    {
        var file = MakeFile(Path.Combine(Path.GetTempPath(), "leftover.tmp"));

        var findings = new TempCacheDetector().Detect([file], []).ToList();

        var finding = Assert.Single(findings);
        Assert.Contains("temp", finding.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Detect_MatchesWindowsTempFolder()
    {
        var windowsTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
        var file = MakeFile(Path.Combine(windowsTemp, "install.log"));

        var findings = new TempCacheDetector().Detect([file], []).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(windowsTemp, finding.TargetPath);
    }

    [Fact]
    public void Detect_MatchesDownloadsFolder()
    {
        var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var file = MakeFile(Path.Combine(downloads, "setup.exe"));

        var findings = new TempCacheDetector().Detect([file], []).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(downloads, finding.TargetPath);
    }

    [Fact]
    public void Detect_MatchesGenericCacheFolderName_AnywhereInTree()
    {
        var file = MakeFile(@"C:\Users\test\AppData\Local\SomeApp\Cache\entry123");

        var findings = new TempCacheDetector().Detect([file], []).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(@"C:\Users\test\AppData\Local\SomeApp\Cache", finding.TargetPath);
    }

    [Fact]
    public void Detect_MatchesPythonPycacheFolder()
    {
        var file = MakeFile(@"C:\proj\myapp\__pycache__\module.cpython-312.pyc");

        var findings = new TempCacheDetector().Detect([file], []).ToList();

        Assert.Single(findings);
    }

    [Fact]
    public void Detect_DoesNotMatchUnrelatedFolder()
    {
        var file = MakeFile(@"C:\Users\test\Documents\report.docx");

        var findings = new TempCacheDetector().Detect([file], []).ToList();

        Assert.Empty(findings);
    }

    [Fact]
    public void Detect_AggregatesSizeAndFileCount_AcrossFilesInSameJunkFolder()
    {
        var files = new[]
        {
            MakeFile(@"C:\proj\Temp\a.tmp", 1000),
            MakeFile(@"C:\proj\Temp\sub\b.tmp", 2000)
        };

        var finding = new TempCacheDetector().Detect(files, []).Single();

        Assert.Equal(@"C:\proj\Temp", finding.TargetPath);
        Assert.Equal(3000, finding.SizeBytes);
        Assert.Contains("2 file(s)", finding.Description);
    }

    [Fact]
    public void Detect_KeepsOnlyOutermostMatch_WhenAJunkFolderContainsAnotherJunkFolder()
    {
        var files = new[]
        {
            MakeFile(@"C:\proj\Temp\build.log"),
            MakeFile(@"C:\proj\Temp\Cache\entry")
        };

        var findings = new TempCacheDetector().Detect(files, []).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(@"C:\proj\Temp", finding.TargetPath);
    }
}
