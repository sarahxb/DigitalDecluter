using DiskReclaimer.Infrastructure.Exclusions;
using Microsoft.Extensions.Logging.Abstractions;

namespace DiskReclaimer.Infrastructure.Tests;

public sealed class ExclusionRuleProviderTests : IDisposable
{
    private readonly string _configFilePath;

    public ExclusionRuleProviderTests()
    {
        _configFilePath = Path.Combine(Path.GetTempPath(), "DiskReclaimerTests_exclusions_" + Guid.NewGuid() + ".json");
    }

    public void Dispose()
    {
        if (File.Exists(_configFilePath))
        {
            File.Delete(_configFilePath);
        }
    }

    [Fact]
    public async Task GetRulesAsync_AlwaysIncludesBuiltInSystemFloorRules()
    {
        var provider = new ExclusionRuleProvider(_configFilePath, NullLogger<ExclusionRuleProvider>.Instance);

        var rules = await provider.GetRulesAsync();

        Assert.All(ExclusionRuleProvider.BuiltInRules, r => Assert.Contains(rules, x => x.PathPattern == r.PathPattern));
        Assert.All(ExclusionRuleProvider.BuiltInRules, r => Assert.True(r.IsSystemFloor));
    }

    [Fact]
    public async Task GetRulesAsync_MergesUserDefinedRulesFromConfigFile()
    {
        await File.WriteAllTextAsync(_configFilePath, """[{"PathPattern": "C:\\Users\\test\\Downloads\\keep", "Reason": "Keep this"}]""");
        var provider = new ExclusionRuleProvider(_configFilePath, NullLogger<ExclusionRuleProvider>.Instance);

        var rules = await provider.GetRulesAsync();

        var userRule = Assert.Single(rules, r => r.PathPattern == @"C:\Users\test\Downloads\keep");
        Assert.False(userRule.IsSystemFloor);
        Assert.Equal("Keep this", userRule.Reason);
    }

    [Fact]
    public async Task GetRulesAsync_IgnoresMalformedConfigFile_AndStillReturnsBuiltInRules()
    {
        await File.WriteAllTextAsync(_configFilePath, "not valid json");
        var provider = new ExclusionRuleProvider(_configFilePath, NullLogger<ExclusionRuleProvider>.Instance);

        var rules = await provider.GetRulesAsync();

        Assert.Equal(ExclusionRuleProvider.BuiltInRules.Count, rules.Count);
    }
}
