using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FileTypeRules;

public sealed class InstallerFileTypeRule() : ExtensionSetFileTypeRule(Category.Installer, [".exe", ".msi"]);
