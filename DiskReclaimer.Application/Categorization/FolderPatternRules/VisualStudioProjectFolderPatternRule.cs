using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FolderPatternRules;

public sealed class VisualStudioProjectFolderPatternRule()
    : MarkerFileFolderPatternRule(FolderType.VisualStudioProject, nameof(VisualStudioProjectFolderPatternRule),
        ["*.sln", "*.csproj"]);
