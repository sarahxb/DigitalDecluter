using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Core.Interfaces;

public interface IExclusionRuleProvider
{
    Task<IReadOnlyList<ExclusionRule>> GetRulesAsync();
}
