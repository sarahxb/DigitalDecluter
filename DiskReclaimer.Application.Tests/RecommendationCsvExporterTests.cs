using DiskReclaimer.Application.Export;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Tests;

public sealed class RecommendationCsvExporterTests
{
    private static Recommendation MakeRecommendation(
        string path, ConfidenceTier tier, long reclaimableBytes, params string[] reasons) =>
        new(path, tier, reclaimableBytes, reasons, []);

    [Fact]
    public void Write_IncludesHeaderRow()
    {
        var csv = RecommendationCsvExporter.Write([]);

        Assert.Equal("Path,Confidence,ReclaimableBytes,Reasons\r\n", csv);
    }

    [Fact]
    public void Write_IncludesOneRowPerRecommendation()
    {
        var recommendation = MakeRecommendation(@"C:\data\big.bin", ConfidenceTier.High, 1_000_000, "Large file");

        var csv = RecommendationCsvExporter.Write([recommendation]);

        Assert.Equal("Path,Confidence,ReclaimableBytes,Reasons\r\nC:\\data\\big.bin,High,1000000,Large file\r\n", csv);
    }

    [Fact]
    public void Write_JoinsMultipleReasons_WithSemicolon()
    {
        var recommendation = MakeRecommendation(@"C:\data\a.bin", ConfidenceTier.Medium, 500, "Large file", "Stale");

        var csv = RecommendationCsvExporter.Write([recommendation]);

        Assert.Contains("Large file; Stale", csv);
    }

    [Fact]
    public void Write_QuotesFieldsContainingCommas()
    {
        var recommendation = MakeRecommendation(@"C:\data\a, b.bin", ConfidenceTier.Low, 100, "reason");

        var csv = RecommendationCsvExporter.Write([recommendation]);

        Assert.Contains("\"C:\\data\\a, b.bin\"", csv);
    }

    [Fact]
    public void Write_EscapesEmbeddedQuotes_ByDoublingThem()
    {
        var recommendation = MakeRecommendation(@"C:\data\a.bin", ConfidenceTier.Low, 100, "has a \"quoted\" word");

        var csv = RecommendationCsvExporter.Write([recommendation]);

        Assert.Contains("\"has a \"\"quoted\"\" word\"", csv);
    }

    [Fact]
    public void Write_DoesNotQuotePlainFields()
    {
        var recommendation = MakeRecommendation(@"C:\data\a.bin", ConfidenceTier.Low, 100, "plain reason");

        var csv = RecommendationCsvExporter.Write([recommendation]);

        Assert.DoesNotContain("\"", csv);
    }
}
