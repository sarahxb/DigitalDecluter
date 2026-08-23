using DiskReclaimer.Core.Models;
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

    [Fact]
    public async Task AddUserRuleAsync_PersistsRule_VisibleOnNextGetRulesAsync()
    {
        var provider = new ExclusionRuleProvider(_configFilePath, NullLogger<ExclusionRuleProvider>.Instance);

        await provider.AddUserRuleAsync(new ExclusionRule(@"C:\data\junk", "Just junk", IsSystemFloor: false), CancellationToken.None);

        var rules = await provider.GetRulesAsync();
        var added = Assert.Single(rules, r => r.PathPattern == @"C:\data\junk");
        Assert.Equal("Just junk", added.Reason);
        Assert.False(added.IsSystemFloor);
    }

    [Fact]
    public async Task AddUserRuleAsync_Throws_WhenRuleIsMarkedSystemFloor()
    {
        var provider = new ExclusionRuleProvider(_configFilePath, NullLogger<ExclusionRuleProvider>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            provider.AddUserRuleAsync(new ExclusionRule(@"C:\data\junk", "nope", IsSystemFloor: true), CancellationToken.None));
    }

    [Fact]
    public async Task AddUserRuleAsync_ReplacesExistingRule_WithSamePathPattern()
    {
        var provider = new ExclusionRuleProvider(_configFilePath, NullLogger<ExclusionRuleProvider>.Instance);
        await provider.AddUserRuleAsync(new ExclusionRule(@"C:\data\junk", "old reason", IsSystemFloor: false), CancellationToken.None);

        await provider.AddUserRuleAsync(new ExclusionRule(@"C:\data\junk", "new reason", IsSystemFloor: false), CancellationToken.None);

        var rules = await provider.GetRulesAsync();
        var rule = Assert.Single(rules, r => r.PathPattern == @"C:\data\junk");
        Assert.Equal("new reason", rule.Reason);
    }

    [Fact]
    public async Task RemoveUserRuleAsync_RemovesOnlyTheMatchingUserRule()
    {
        var provider = new ExclusionRuleProvider(_configFilePath, NullLogger<ExclusionRuleProvider>.Instance);
        await provider.AddUserRuleAsync(new ExclusionRule(@"C:\data\keep", "keep this", IsSystemFloor: false), CancellationToken.None);
        await provider.AddUserRuleAsync(new ExclusionRule(@"C:\data\remove", "remove this", IsSystemFloor: false), CancellationToken.None);

        await provider.RemoveUserRuleAsync(@"C:\data\remove", CancellationToken.None);

        var rules = await provider.GetRulesAsync();
        Assert.Contains(rules, r => r.PathPattern == @"C:\data\keep");
        Assert.DoesNotContain(rules, r => r.PathPattern == @"C:\data\remove");
    }

    [Fact]
    public async Task RemoveUserRuleAsync_CannotRemoveBuiltInRules()
    {
        var provider = new ExclusionRuleProvider(_configFilePath, NullLogger<ExclusionRuleProvider>.Instance);
        var builtInPattern = ExclusionRuleProvider.BuiltInRules[0].PathPattern;

        await provider.RemoveUserRuleAsync(builtInPattern, CancellationToken.None);

        var rules = await provider.GetRulesAsync();
        Assert.Contains(rules, r => r.PathPattern == builtInPattern && r.IsSystemFloor);
    }
}
