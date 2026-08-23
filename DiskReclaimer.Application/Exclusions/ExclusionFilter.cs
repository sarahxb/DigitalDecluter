using System.Text.RegularExpressions;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Exclusions;

/// <summary>
/// Applies exclusion rules once, centrally, right after categorization — everything downstream
/// (detectors, insights) only ever sees the scoped result.
/// </summary>
public static class ExclusionFilter
{
    public static CategorizationResult Apply(CategorizationResult result, IReadOnlyList<ExclusionRule> rules)
    {
        if (rules.Count == 0)
        {
            return result;
        }

        var matchers = rules.Select(r => new CompiledRule(r)).ToList();

        var files = result.Files.Where(f => !matchers.Any(m => m.IsMatch(f.Record.FullPath))).ToList();
        var folders = result.Folders.Where(f => !matchers.Any(m => m.IsMatch(f.Path))).ToList();

        return new CategorizationResult(files, folders);
    }

    private sealed class CompiledRule
    {
        private readonly Regex? _globRegex;
        private readonly string? _prefix;

        public CompiledRule(ExclusionRule rule)
        {
            if (rule.PathPattern.Contains('*') || rule.PathPattern.Contains('?'))
            {
                var escaped = Regex.Escape(rule.PathPattern)
                    .Replace(@"\*", ".*")
                    .Replace(@"\?", ".");
                _globRegex = new Regex($"^{escaped}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            else
            {
                _prefix = rule.PathPattern.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
        }

        public bool IsMatch(string path)
        {
            if (_globRegex is not null)
            {
                return _globRegex.IsMatch(path);
            }

            return path.Equals(_prefix, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(_prefix + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
    }
}
