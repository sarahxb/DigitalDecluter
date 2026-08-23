using System.IO;
using DiskReclaimer.UI.ViewModels;

namespace DiskReclaimer.UI.Tests;

public sealed class MainViewModelTests : IDisposable
{
    private readonly string _tempFile;
    private readonly string _tempDirectory;

    public MainViewModelTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), "DiskReclaimerUiTests_" + Guid.NewGuid() + ".txt");
        File.WriteAllText(_tempFile, "x");

        _tempDirectory = Path.Combine(Path.GetTempPath(), "DiskReclaimerUiTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }

        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void BuildRevealArgument_ForExistingFile_UsesSelectSwitch()
    {
        var argument = MainViewModel.BuildRevealArgument(_tempFile);

        Assert.Equal($"/select,{_tempFile}", argument);
    }

    [Fact]
    public void BuildRevealArgument_ForExistingDirectory_OpensItDirectly()
    {
        var argument = MainViewModel.BuildRevealArgument(_tempDirectory);

        Assert.Equal(_tempDirectory, argument);
    }

    [Fact]
    public void BuildRevealArgument_ForPathThatNoLongerExists_ReturnsNull()
    {
        var missingPath = Path.Combine(_tempDirectory, "does-not-exist-" + Guid.NewGuid());

        var argument = MainViewModel.BuildRevealArgument(missingPath);

        Assert.Null(argument);
    }
}
