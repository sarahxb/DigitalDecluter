using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Core.Interfaces;

public interface IExclusionRuleProvider
{
    Task<IReadOnlyList<ExclusionRule>> GetRulesAsync();

    /// <summary>Adds a user-defined rule, replacing any existing user rule with the same PathPattern.</summary>
    Task AddUserRuleAsync(ExclusionRule rule, CancellationToken cancellationToken);

    /// <summary>Removes the user-defined rule with the given PathPattern, if any. Built-in rules can't be removed.</summary>
    Task RemoveUserRuleAsync(string pathPattern, CancellationToken cancellationToken);
}
