using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FolderPatternRules;

public sealed class IntelliJProjectFolderPatternRule()
    : MarkerFileFolderPatternRule(FolderType.IntelliJProject, nameof(IntelliJProjectFolderPatternRule), ["*.iml"]);
