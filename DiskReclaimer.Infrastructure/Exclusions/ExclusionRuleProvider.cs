using System.Text.Json;
using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;
using Microsoft.Extensions.Logging;

namespace DiskReclaimer.Infrastructure.Exclusions;

/// <summary>
/// Supplies the exclusion rule list: a hardcoded system floor (Windows/Program Files/AppData system
/// folders) that no detector can override, plus user-added rules read from a JSON config file.
/// </summary>
public sealed class ExclusionRuleProvider : IExclusionRuleProvider
{
    private readonly string _configFilePath;
    private readonly ILogger<ExclusionRuleProvider> _logger;

    public ExclusionRuleProvider(string configFilePath, ILogger<ExclusionRuleProvider> logger)
    {
        _configFilePath = configFilePath;
        _logger = logger;
    }

    public static IReadOnlyList<ExclusionRule> BuiltInRules { get; } = BuildBuiltInRules();

    public async Task<IReadOnlyList<ExclusionRule>> GetRulesAsync()
    {
        var rules = new List<ExclusionRule>(BuiltInRules);
        rules.AddRange(await LoadUserRulesAsync());
        return rules;
    }

    private async Task<IReadOnlyList<ExclusionRule>> LoadUserRulesAsync()
    {
        if (!File.Exists(_configFilePath))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(_configFilePath);
            var entries = await JsonSerializer.DeserializeAsync<List<UserExclusionEntry>>(stream) ?? [];
            return entries
                .Select(e => new ExclusionRule(e.PathPattern, e.Reason ?? "User-defined exclusion", IsSystemFloor: false))
                .ToList();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            _logger.LogWarning(ex, "Failed to read user exclusion config at {Path}; ignoring user rules", _configFilePath);
            return [];
        }
    }

    private static List<ExclusionRule> BuildBuiltInRules()
    {
        var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var rules = new List<ExclusionRule>
        {
            new(windowsDir, "Windows system directory", IsSystemFloor: true),
            new(programFiles, "Program Files", IsSystemFloor: true),
            new(programData, "ProgramData", IsSystemFloor: true),
            new(Path.Combine(localAppData, "Microsoft", "Windows"), "Windows per-user system data", IsSystemFloor: true),
            new(Path.Combine(appData, "Microsoft", "Windows"), "Windows per-user system data", IsSystemFloor: true),
            new(@"$Recycle.Bin", "Recycle Bin", IsSystemFloor: true),
            new(@"System Volume Information", "System Volume Information", IsSystemFloor: true),
        };

        if (!string.IsNullOrEmpty(programFilesX86))
        {
            rules.Add(new ExclusionRule(programFilesX86, "Program Files (x86)", IsSystemFloor: true));
        }

        return rules;
    }

    private sealed class UserExclusionEntry
    {
        public string PathPattern { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}
