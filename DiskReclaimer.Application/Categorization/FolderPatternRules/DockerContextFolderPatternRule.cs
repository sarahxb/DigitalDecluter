using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FolderPatternRules;

public sealed class DockerContextFolderPatternRule()
    : MarkerFileFolderPatternRule(FolderType.DockerContext, nameof(DockerContextFolderPatternRule),
        ["Dockerfile", "docker-compose.yml", "docker-compose.yaml"]);
