using System.Windows;
using MDOlusturucu.ViewModels;
using WinForms = System.Windows.Forms;

namespace MDOlusturucu.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = _vm;

        _vm.BrowseFolderRequested += BrowseFolder;
        _vm.CloseRequested        += () => Close();
    }

    private void BrowseFolder()
    {
        using var dialog = new WinForms.FolderBrowserDialog
        {
            Description            = L.Get("settings_folder_dialog"),
            SelectedPath           = _vm.MarkdownDirectory ?? string.Empty,
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            _vm.MarkdownDirectory = dialog.SelectedPath;
    }
}
