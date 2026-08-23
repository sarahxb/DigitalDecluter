using DiskReclaimer.Application.Categorization;
using DiskReclaimer.Application.Categorization.FileTypeRules;
using DiskReclaimer.Application.Categorization.FolderPatternRules;
using DiskReclaimer.Application.Detectors;
using DiskReclaimer.Application.Insights;
using DiskReclaimer.Application.Recommendations;
using DiskReclaimer.Application.Scanning;
using DiskReclaimer.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DiskReclaimer.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiskReclaimerApplication(this IServiceCollection services)
    {
        services.AddSingleton<IReadOnlyList<IFileTypeRule>>(
        [
            new SystemJunkFileTypeRule(),
            new InstallerFileTypeRule(),
            new ArchiveFileTypeRule(),
            new MediaFileTypeRule(),
            new DocumentFileTypeRule(),
            new CodeProjectFileTypeRule(),
            new LogFileTypeRule(),
            new TempFileTypeRule()
        ]);

        services.AddSingleton<IReadOnlyList<IFolderPatternRule>>(
        [
            new GitRepoFolderPatternRule(),
            new NodeModulesFolderPatternRule(),
            new PythonVenvFolderPatternRule(),
            new VisualStudioProjectFolderPatternRule(),
            new IntelliJProjectFolderPatternRule(),
            new DockerContextFolderPatternRule(),
            new BuildOutputFolderPatternRule()
        ]);

        services.AddSingleton<IReadOnlyList<IRecommendationDetector>>(
        [
            new LargeFileDetector(),
            new DuplicateFileDetector(),
            new StaleFileDetector(),
            new TempCacheDetector(),
            new InstallerDetector(),
            new OldProjectDetector()
        ]);

        services.AddSingleton<ICategorizer, Categorizer>();
        services.AddSingleton<IRecommendationEngine, RecommendationEngine>();
        services.AddSingleton<IFolderInsightsService, FolderInsightsService>();
        services.AddSingleton<IScanOrchestrator, ScanOrchestrator>();

        return services;
    }
}
