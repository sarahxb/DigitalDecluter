using DiskReclaimer.Application.Categorization.FolderPatternRules;
using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Tests;

public sealed class FolderPatternRuleTests
{
    private static FileRecord MakeFile(string directory, string name)
    {
        var now = DateTimeOffset.UtcNow;
        var fullPath = Path.Combine(directory, name);
        return new FileRecord(fullPath, name, Path.GetExtension(name), 1, now, now, now);
    }

    [Fact]
    public void GitRepoFolderPatternRule_MatchesFolderNamedDotGit_RegardlessOfContents()
    {
        var rule = new GitRepoFolderPatternRule();

        var match = rule.Match(@"C:\repo\.git", []);

        Assert.NotNull(match);
        Assert.Equal(FolderType.GitRepo, match!.FolderType);
    }

    [Fact]
    public void GitRepoFolderPatternRule_DoesNotMatchUnrelatedFolderName()
    {
        var rule = new GitRepoFolderPatternRule();

        var match = rule.Match(@"C:\repo\src", []);

        Assert.Null(match);
    }

    [Fact]
    public void VisualStudioProjectFolderPatternRule_MatchesOnCsprojMarker()
    {
        var rule = new VisualStudioProjectFolderPatternRule();
        var files = new[] { MakeFile(@"C:\proj\App", "App.csproj"), MakeFile(@"C:\proj\App", "Program.cs") };

        var match = rule.Match(@"C:\proj\App", files);

        Assert.NotNull(match);
        Assert.Equal(FolderType.VisualStudioProject, match!.FolderType);
    }

    [Fact]
    public void VisualStudioProjectFolderPatternRule_DoesNotMatchWithoutMarker()
    {
        var rule = new VisualStudioProjectFolderPatternRule();
        var files = new[] { MakeFile(@"C:\proj\App", "Program.cs") };

        var match = rule.Match(@"C:\proj\App", files);

        Assert.Null(match);
    }

    [Fact]
    public void DockerContextFolderPatternRule_MatchesOnDockerfile()
    {
        var rule = new DockerContextFolderPatternRule();
        var files = new[] { MakeFile(@"C:\proj\api", "Dockerfile") };

        var match = rule.Match(@"C:\proj\api", files);

        Assert.NotNull(match);
        Assert.Equal(FolderType.DockerContext, match!.FolderType);
    }
}
