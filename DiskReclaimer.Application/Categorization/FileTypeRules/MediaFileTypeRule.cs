using DiskReclaimer.Core.Models;

namespace DiskReclaimer.Application.Categorization.FileTypeRules;

public sealed class MediaFileTypeRule() : ExtensionSetFileTypeRule(Category.Media,
[
    ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".heic", ".svg",
    ".mp4", ".mov", ".avi", ".mkv", ".wmv",
    ".mp3", ".wav", ".flac", ".aac"
]);
