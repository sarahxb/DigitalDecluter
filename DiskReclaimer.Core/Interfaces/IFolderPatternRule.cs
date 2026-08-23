using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Core.Interfaces;

public interface IFolderPatternRule
{
    DetectedFolder? Match(string folderPath, IReadOnlyList<FileRecord> filesInFolder);
}
