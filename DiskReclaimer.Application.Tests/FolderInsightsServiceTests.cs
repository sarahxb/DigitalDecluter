using DiskReclaimer.Application.Insights;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Tests;

public sealed class FolderInsightsServiceTests
{
    private static CategorizedFile MakeFile(string fullPath, Category category, long sizeBytes)
    {
        var now = DateTimeOffset.UtcNow;
        var record = new FileRecord(fullPath, Path.GetFileName(fullPath), Path.GetExtension(fullPath), sizeBytes, now, now, now);
        return new CategorizedFile(record, category, null);
    }

    private static DetectedFolder MakeFolder(string path, long aggregateSizeBytes, int fileCount) =>
        new(path, FolderType.GitRepo, "rule", aggregateSizeBytes, fileCount, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    [Fact]
    public void Summarize_ReturnsOneInsightPerDetectedFolder()
    {
        var folders = new[]
        {
            MakeFolder(@"C:\repo1", 1000, 2),
            MakeFolder(@"C:\repo2", 2000, 3)
        };

        var insights = new FolderInsightsService().Summarize([], folders);

        Assert.Equal(2, insights.Count);
        Assert.Contains(insights, i => i.FolderPath == @"C:\repo1");
        Assert.Contains(insights, i => i.FolderPath == @"C:\repo2");
    }

    [Fact]
    public void Summarize_UsesFolderAggregates_ForTotalSizeAndFileCount()
    {
        var folder = MakeFolder(@"C:\repo", aggregateSizeBytes: 5_000_000, fileCount: 42);

        var insight = new FolderInsightsService().Summarize([], [folder]).Single();

        Assert.Equal(5_000_000, insight.TotalSizeBytes);
        Assert.Equal(42, insight.FileCount);
    }

    [Fact]
    public void Summarize_BreaksDownSizeByCategory_ForFilesUnderTheFolder()
    {
        var folder = MakeFolder(@"C:\repo", 1500, 3);
        var files = new[]
        {
            MakeFile(@"C:\repo\a.cs", Category.CodeProject, 500),
            MakeFile(@"C:\repo\b.cs", Category.CodeProject, 500),
            MakeFile(@"C:\repo\logo.png", Category.Media, 500)
        };

        var insight = new FolderInsightsService().Summarize(files, [folder]).Single();

        Assert.Equal(1000, insight.CategoryBreakdown[Category.CodeProject]);
        Assert.Equal(500, insight.CategoryBreakdown[Category.Media]);
    }

    [Fact]
    public void Summarize_ExcludesFilesOutsideTheFolder_FromCategoryBreakdown()
    {
        var folder = MakeFolder(@"C:\repo", 500, 1);
        var files = new[]
        {
            MakeFile(@"C:\repo\a.cs", Category.CodeProject, 500),
            MakeFile(@"C:\other\b.cs", Category.CodeProject, 999)
        };

        var insight = new FolderInsightsService().Summarize(files, [folder]).Single();

        Assert.Equal(500, insight.CategoryBreakdown[Category.CodeProject]);
    }

    [Fact]
    public void Summarize_ReturnsEmpty_WhenNoFoldersDetected()
    {
        var files = new[] { MakeFile(@"C:\data\a.txt", Category.Document, 100) };

        var insights = new FolderInsightsService().Summarize(files, []);

        Assert.Empty(insights);
    }
}
