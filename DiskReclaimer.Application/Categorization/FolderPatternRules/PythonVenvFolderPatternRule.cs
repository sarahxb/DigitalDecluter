using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FolderPatternRules;

public sealed class PythonVenvFolderPatternRule()
    : FolderNameFolderPatternRule(FolderType.PythonVenv, nameof(PythonVenvFolderPatternRule), [".venv", "venv", "env"]);
