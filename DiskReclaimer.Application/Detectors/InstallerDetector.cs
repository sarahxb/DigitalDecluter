using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Detectors;

/// <summary>
/// Flags files that look like installer packages: MSI packages and disk images unconditionally (both
/// formats are used for almost nothing else), EXE/ZIP files only when their name carries an
/// installer-ish keyword — otherwise every ordinary standalone .exe (a dev tool, a portable app)
/// would get flagged, which would swamp the recommendation list with false positives.
/// </summary>
public sealed class InstallerDetector : IRecommendationDetector
{
    private static readonly string[] InstallerNameKeywords = ["setup", "install", "installer"];

    public IEnumerable<Finding> Detect(IReadOnlyList<CategorizedFile> files, IReadOnlyList<DetectedFolder> folders)
    {
        foreach (var file in files)
        {
            var description = DescribeIfInstaller(file.Record);
            if (description is not null)
            {
                yield return new Finding(file.Record.FullPath, nameof(InstallerDetector), description, file.Record.SizeBytes);
            }
        }
    }

    private static string? DescribeIfInstaller(FileRecord record)
    {
        var extension = record.Extension;

        if (string.Equals(extension, ".msi", StringComparison.OrdinalIgnoreCase))
        {
            return "Installer package (.msi)";
        }

        if (string.Equals(extension, ".iso", StringComparison.OrdinalIgnoreCase))
        {
            return "Disk image (.iso), often installer media";
        }

        if (string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase))
        {
            var keyword = InstallerNameKeywords.FirstOrDefault(k => record.Name.Contains(k, StringComparison.OrdinalIgnoreCase));
            if (keyword is not null)
            {
                return $"Installer-named {extension.TrimStart('.').ToUpperInvariant()} file (matched \"{keyword}\")";
            }
        }

        return null;
    }
}
