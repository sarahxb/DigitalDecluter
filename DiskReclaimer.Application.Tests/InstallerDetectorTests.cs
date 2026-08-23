using DiskReclaimer.Application.Detectors;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Tests;

public sealed class InstallerDetectorTests
{
    private static CategorizedFile MakeFile(string name)
    {
        var now = DateTimeOffset.UtcNow;
        var record = new FileRecord(@"C:\Downloads\" + name, name, Path.GetExtension(name), 1000, now, now, now);
        return new CategorizedFile(record, Category.Unknown, null);
    }

    [Theory]
    [InlineData("package.msi")]
    [InlineData("PACKAGE.MSI")]
    public void Detect_AlwaysFlagsMsiFiles(string name)
    {
        var findings = new InstallerDetector().Detect([MakeFile(name)], []).ToList();

        var finding = Assert.Single(findings);
        Assert.Contains("msi", finding.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Detect_AlwaysFlagsIsoFiles()
    {
        var findings = new InstallerDetector().Detect([MakeFile("ubuntu-24.04.iso")], []).ToList();

        Assert.Single(findings);
    }

    [Theory]
    [InlineData("VSCodeUserSetup-x64-1.85.0.exe")]
    [InlineData("python-3.12.0-amd64-install.exe")]
    [InlineData("node-installer.exe")]
    [InlineData("INSTALL.EXE")]
    public void Detect_FlagsExeFiles_WithInstallerKeywordInName(string name)
    {
        var findings = new InstallerDetector().Detect([MakeFile(name)], []).ToList();

        Assert.Single(findings);
    }

    [Fact]
    public void Detect_DoesNotFlagPlainExeWithoutInstallerKeyword()
    {
        var findings = new InstallerDetector().Detect([MakeFile("ffmpeg.exe")], []).ToList();

        Assert.Empty(findings);
    }

    [Fact]
    public void Detect_FlagsZipFiles_WithInstallerKeywordInName()
    {
        var findings = new InstallerDetector().Detect([MakeFile("app-installer-bundle.zip")], []).ToList();

        Assert.Single(findings);
    }

    [Fact]
    public void Detect_DoesNotFlagPlainZipWithoutInstallerKeyword()
    {
        var findings = new InstallerDetector().Detect([MakeFile("photos-backup.zip")], []).ToList();

        Assert.Empty(findings);
    }

    [Theory]
    [InlineData("report.pdf")]
    [InlineData("notes.txt")]
    [InlineData("archive.rar")]
    public void Detect_DoesNotFlagUnrelatedExtensions(string name)
    {
        var findings = new InstallerDetector().Detect([MakeFile(name)], []).ToList();

        Assert.Empty(findings);
    }

    [Fact]
    public void Detect_ReportsFileSizeAsReclaimableBytes()
    {
        var finding = new InstallerDetector().Detect([MakeFile("setup.msi")], []).Single();

        Assert.Equal(1000, finding.SizeBytes);
    }
}
