using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FolderPatternRules;

public sealed class GitRepoFolderPatternRule()
    : FolderNameFolderPatternRule(FolderType.GitRepo, nameof(GitRepoFolderPatternRule), [".git"]);
