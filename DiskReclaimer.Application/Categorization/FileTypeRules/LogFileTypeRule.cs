using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FileTypeRules;

public sealed class LogFileTypeRule() : ExtensionSetFileTypeRule(Category.Log, [".log", ".dmp", ".mdmp", ".etl"]);
