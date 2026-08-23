using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FileTypeRules;

public sealed class DocumentFileTypeRule() : ExtensionSetFileTypeRule(Category.Document,
[
    ".doc", ".docx", ".pdf", ".txt", ".xls", ".xlsx", ".ppt", ".pptx", ".md", ".rtf", ".odt"
]);
