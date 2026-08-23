using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FolderPatternRules;

public sealed class BuildOutputFolderPatternRule()
    : FolderNameFolderPatternRule(FolderType.BuildOutput, nameof(BuildOutputFolderPatternRule),
        ["bin", "obj", "dist", "build", "out", "target"]);
