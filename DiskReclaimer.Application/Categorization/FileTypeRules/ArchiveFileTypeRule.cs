using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FileTypeRules;

public sealed class ArchiveFileTypeRule()
    : ExtensionSetFileTypeRule(Category.Archive, [".zip", ".rar", ".7z", ".tar", ".gz", ".iso"]);
