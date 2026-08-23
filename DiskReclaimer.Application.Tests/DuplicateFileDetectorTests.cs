using DiskReclaimer.Application.Detectors;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Tests;

public sealed class DuplicateFileDetectorTests : IDisposable
{
    private readonly string _root;

    public DuplicateFileDetectorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "DiskReclaimerTests_dup_" + Guid.NewGuid());
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private CategorizedFile WriteFile(string name, string content)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, content);
        var info = new FileInfo(path);
        var record = new FileRecord(
            info.FullName, info.Name, info.Extension, info.Length,
            info.CreationTimeUtc, info.LastWriteTimeUtc, info.LastAccessTimeUtc);
        return new CategorizedFile(record, Category.Unknown, null);
    }

    [Fact]
    public void Detect_FlagsFilesWithIdenticalContent_AsDuplicatesOfEachOther()
    {
        var a = WriteFile("a.txt", "identical content, padded to a decent length for hashing");
        var b = WriteFile("b.txt", "identical content, padded to a decent length for hashing");
        var detector = new DuplicateFileDetector();

        var findings = detector.Detect([a, b], []).Cast<DuplicateFinding>().ToList();

        Assert.Equal(2, findings.Count);
        Assert.All(findings, f => Assert.Equal(f.SizeBytes, a.Record.SizeBytes));
        var groupIds = findings.Select(f => f.DuplicateGroupId).Distinct().ToList();
        Assert.Single(groupIds);

        var aFinding = findings.Single(f => f.TargetPath == a.Record.FullPath);
        Assert.Equal([b.Record.FullPath], aFinding.OtherPaths);
    }

    [Fact]
    public void Detect_DoesNotFlagFilesWithDifferentContent_EvenWhenSameSize()
    {
        var a = WriteFile("a.txt", "AAAAAAAAAA");
        var b = WriteFile("b.txt", "BBBBBBBBBB");

        var findings = new DuplicateFileDetector().Detect([a, b], []).ToList();

        Assert.Empty(findings);
    }

    [Fact]
    public void Detect_DoesNotFlagFilesWithUniqueSizes()
    {
        var a = WriteFile("a.txt", "short");
        var b = WriteFile("b.txt", "a much much much longer piece of content than the other file");

        var findings = new DuplicateFileDetector().Detect([a, b], []).ToList();

        Assert.Empty(findings);
    }

    [Fact]
    public void Detect_GroupsThreeIdenticalFilesTogether()
    {
        var a = WriteFile("a.txt", "triplicate content here");
        var b = WriteFile("b.txt", "triplicate content here");
        var c = WriteFile("c.txt", "triplicate content here");

        var findings = new DuplicateFileDetector().Detect([a, b, c], []).Cast<DuplicateFinding>().ToList();

        Assert.Equal(3, findings.Count);
        Assert.All(findings, f => Assert.Equal(2, f.OtherPaths.Count));
        Assert.Single(findings.Select(f => f.DuplicateGroupId).Distinct());
    }

    [Fact]
    public void Detect_ExcludesFilesBelowMinimumSize_ByDefaultZeroByteFiles()
    {
        var a = WriteFile("a.txt", string.Empty);
        var b = WriteFile("b.txt", string.Empty);

        var findings = new DuplicateFileDetector().Detect([a, b], []).ToList();

        Assert.Empty(findings);
    }

    [Fact]
    public void Detect_ProducesSeparateGroupIds_ForUnrelatedDuplicateSets()
    {
        var a1 = WriteFile("a1.txt", "group A content");
        var a2 = WriteFile("a2.txt", "group A content");
        var b1 = WriteFile("b1.txt", "group B content!!");
        var b2 = WriteFile("b2.txt", "group B content!!");

        var findings = new DuplicateFileDetector().Detect([a1, a2, b1, b2], []).Cast<DuplicateFinding>().ToList();

        Assert.Equal(4, findings.Count);
        var groupIds = findings.Select(f => f.DuplicateGroupId).Distinct().ToList();
        Assert.Equal(2, groupIds.Count);
    }
}
