using System.Globalization;
using System.Text;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Export;

/// <summary>Pure formatter: turns recommendations into CSV text. Writing that text to disk is the UI's job.</summary>
public static class RecommendationCsvExporter
{
    private static readonly string[] Header = ["Path", "Confidence", "ReclaimableBytes", "Reasons"];

    public static string Write(IReadOnlyList<Recommendation> recommendations)
    {
        var builder = new StringBuilder();
        builder.Append(string.Join(",", Header)).Append("\r\n");

        foreach (var recommendation in recommendations)
        {
            string[] fields =
            [
                recommendation.TargetPath,
                recommendation.ConfidenceTier.ToString(),
                recommendation.ReclaimableBytes.ToString(CultureInfo.InvariantCulture),
                string.Join("; ", recommendation.Reasons)
            ];

            builder.Append(string.Join(",", fields.Select(EscapeField))).Append("\r\n");
        }

        return builder.ToString();
    }

    private static string EscapeField(string field)
    {
        var needsQuoting = field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r');
        return needsQuoting ? $"\"{field.Replace("\"", "\"\"")}\"" : field;
    }
}
