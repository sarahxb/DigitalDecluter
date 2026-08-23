using System.Windows;
using DiskReclaimer.UI.ViewModels;
using Microsoft.Win32;

namespace DiskReclaimer.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }

    private void OnBrowseClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Select a folder to scan" };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.RootPath = dialog.FolderName;
        }
    }

    private void OnExportCsvClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export recommendations to CSV",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = "disk-reclaimer-recommendations.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.ExportRecommendationsToCsv(dialog.FileName);
        }
    }
}
