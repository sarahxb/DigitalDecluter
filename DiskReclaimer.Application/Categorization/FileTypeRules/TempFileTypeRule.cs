using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FileTypeRules;

public sealed class TempFileTypeRule() : ExtensionSetFileTypeRule(Category.Temp, [".tmp", ".temp", ".bak", ".old", ".cache"]);
