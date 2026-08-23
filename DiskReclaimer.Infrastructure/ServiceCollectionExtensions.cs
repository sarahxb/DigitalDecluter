using DiskReclaimer.Core.Interfaces;
using DiskReclaimer.Infrastructure.Deletion;
using DiskReclaimer.Infrastructure.Exclusions;
using DiskReclaimer.Infrastructure.Persistence;
using DiskReclaimer.Infrastructure.Scanning;
using Microsoft.Extensions.DependencyInjection;

namespace DiskReclaimer.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiskReclaimerInfrastructure(
        this IServiceCollection services, string exclusionConfigFilePath, string scanHistoryDatabaseFilePath)
    {
        services.AddSingleton<IFileScanner, FileScanner>();
        services.AddSingleton<IExclusionRuleProvider>(sp =>
            new ExclusionRuleProvider(exclusionConfigFilePath, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ExclusionRuleProvider>>()));
        services.AddSingleton<IScanHistoryStore>(_ => new SqliteScanHistoryStore(scanHistoryDatabaseFilePath));
        services.AddSingleton<IRecycleBinService, RecycleBinService>();

        return services;
    }
}
