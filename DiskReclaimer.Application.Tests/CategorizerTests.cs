using DiskReclaimer.Application.Categorization;
using DiskReclaimer.Application.Categorization.FileTypeRules;
using DiskReclaimer.Application.Categorization.FolderPatternRules;
using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Tests;

public sealed class CategorizerTests
{
    private static FileRecord MakeFile(string fullPath, long sizeBytes = 100, int daysOld = 1)
    {
        var timestamp = DateTimeOffset.UtcNow.AddDays(-daysOld);
        return new FileRecord(
            fullPath,
            Path.GetFileName(fullPath),
            Path.GetExtension(fullPath),
            sizeBytes,
            timestamp,
            timestamp,
            timestamp);
    }

    private static Categorizer BuildDefaultCategorizer()
    {
        IReadOnlyList<IFileTypeRule> fileTypeRules =
        [
            new SystemJunkFileTypeRule(),
            new InstallerFileTypeRule(),
            new ArchiveFileTypeRule(),
            new MediaFileTypeRule(),
            new DocumentFileTypeRule(),
            new CodeProjectFileTypeRule(),
            new LogFileTypeRule(),
            new TempFileTypeRule()
        ];

        IReadOnlyList<IFolderPatternRule> folderPatternRules =
        [
            new GitRepoFolderPatternRule(),
            new NodeModulesFolderPatternRule(),
            new PythonVenvFolderPatternRule(),
            new VisualStudioProjectFolderPatternRule(),
            new IntelliJProjectFolderPatternRule(),
            new DockerContextFolderPatternRule(),
            new BuildOutputFolderPatternRule()
        ];

        return new Categorizer(fileTypeRules, folderPatternRules);
    }

    [Fact]
    public void Categorize_AssignsCategoryByExtension()
    {
        var categorizer = BuildDefaultCategorizer();
        var files = new[] { MakeFile(@"C:\data\report.pdf"), MakeFile(@"C:\data\photo.jpg"), MakeFile(@"C:\data\setup.exe") };

        var result = categorizer.Categorize(files);

        Assert.Equal(Category.Document, result.Files.Single(f => f.Record.Name == "report.pdf").Category);
        Assert.Equal(Category.Media, result.Files.Single(f => f.Record.Name == "photo.jpg").Category);
        Assert.Equal(Category.Installer, result.Files.Single(f => f.Record.Name == "setup.exe").Category);
    }

    [Fact]
    public void Categorize_FallsBackToUnknown_WhenNoRuleMatches()
    {
        var categorizer = BuildDefaultCategorizer();
        var files = new[] { MakeFile(@"C:\data\mystery.xyz") };

        var result = categorizer.Categorize(files);

        Assert.Equal(Category.Unknown, result.Files.Single().Category);
    }

    [Fact]
    public void Categorize_DetectsNodeModulesFolder_ByName()
    {
        var categorizer = BuildDefaultCategorizer();
        var files = new[] { MakeFile(@"C:\proj\node_modules\lodash\index.js") };

        var result = categorizer.Categorize(files);

        var folder = Assert.Single(result.Folders);
        Assert.Equal(FolderType.NodeModules, folder.FolderType);
        Assert.Equal(@"C:\proj\node_modules", folder.Path);
    }

    [Fact]
    public void Categorize_DetectsVisualStudioProject_ByCsprojMarker()
    {
        var categorizer = BuildDefaultCategorizer();
        var files = new[]
        {
            MakeFile(@"C:\proj\App\App.csproj"),
            MakeFile(@"C:\proj\App\Program.cs")
        };

        var result = categorizer.Categorize(files);

        var folder = Assert.Single(result.Folders);
        Assert.Equal(FolderType.VisualStudioProject, folder.FolderType);
    }

    [Fact]
    public void Categorize_ComputesAggregateSizeAndFileCount_ForDetectedFolder()
    {
        var categorizer = BuildDefaultCategorizer();
        var files = new[]
        {
            MakeFile(@"C:\proj\node_modules\pkg-a\index.js", sizeBytes: 1000),
            MakeFile(@"C:\proj\node_modules\pkg-b\index.js", sizeBytes: 2000),
        };

        var result = categorizer.Categorize(files);

        var folder = Assert.Single(result.Folders);
        Assert.Equal(3000, folder.AggregateSizeBytes);
        Assert.Equal(2, folder.FileCount);
    }

    [Fact]
    public void Categorize_KeepsOnlyOutermostDetectedFolder_WhenSameFolderTypeIsNested()
    {
        var categorizer = BuildDefaultCategorizer();
        var files = new[]
        {
            // Outer node_modules, plus a package with its own nested node_modules — both directories
            // directly contain a file (npm's .package-lock.json) so both would match on their own.
            MakeFile(@"C:\proj\node_modules\.package-lock.json"),
            MakeFile(@"C:\proj\node_modules\pkg-a\node_modules\.package-lock.json")
        };

        var result = categorizer.Categorize(files);

        var folder = Assert.Single(result.Folders);
        Assert.Equal(@"C:\proj\node_modules", folder.Path);
    }
}
