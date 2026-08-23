using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FileTypeRules;

public sealed class CodeProjectFileTypeRule() : ExtensionSetFileTypeRule(Category.CodeProject,
[
    ".cs", ".csproj", ".sln", ".java", ".py", ".js", ".ts", ".jsx", ".tsx",
    ".cpp", ".c", ".h", ".hpp", ".go", ".rb", ".php", ".rs", ".kt", ".swift",
    ".html", ".css", ".json", ".xml", ".yml", ".yaml"
]);
