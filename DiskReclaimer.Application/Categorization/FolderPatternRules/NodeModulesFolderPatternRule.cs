using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FolderPatternRules;

public sealed class NodeModulesFolderPatternRule()
    : FolderNameFolderPatternRule(FolderType.NodeModules, nameof(NodeModulesFolderPatternRule), ["node_modules"]);
