using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MDOlusturucu.Models;
using MDOlusturucu.Services;

namespace MDOlusturucu.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileService     _fileService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private ObservableCollection<FileModel> _files = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedFile))]
    private FileModel? _selectedFile;

    [ObservableProperty]
    private bool _isDarkTheme = true;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _editorFontFamily = "Cascadia Code";

    [ObservableProperty]
    private string _editorFontColor = "";

    public bool HasSelectedFile => SelectedFile is not null;

    public event Action?         OpenFileRequested;
    public event Action?         OpenSettingsRequested;
    public event Action<FileModel>? BeginRenameRequested;

    public MainViewModel(IFileService fileService, ISettingsService settingsService)
    {
        _fileService     = fileService;
        _settingsService = settingsService;

        StatusMessage = L.Get("status_default");

        ApplySettings(_settingsService.Current);
        _settingsService.SettingsChanged += (_, s) =>
        {
            ApplySettings(s);
            LoadFiles();
        };

        LoadFiles();
    }

    private void ApplySettings(AppSettings s)
    {
        EditorFontFamily = string.IsNullOrWhiteSpace(s.EditorFontFamily)
            ? "Cascadia Code"
            : s.EditorFontFamily;

        EditorFontColor = s.EditorFontColor ?? "";
    }

    partial void OnSelectedFileChanged(FileModel? value)
    {
        StatusMessage = value?.Name ?? L.Get("status_default");
    }

    public void LoadFiles()
    {
        var dir = _settingsService.Current.MarkdownDirectory;
        var loaded = _fileService.GetFiles(dir);
        Files = new ObservableCollection<FileModel>(loaded);
        SelectedFile = Files.FirstOrDefault();
    }

    [RelayCommand]
    private void SaveFile()
    {
        if (SelectedFile is null) return;
        _fileService.Write(SelectedFile);
        StatusMessage = $"{SelectedFile.Name}  —  {L.Get("status_saved")}";
    }

    [RelayCommand]
    private void NewFile()
    {
        var dir = _settingsService.Current.MarkdownDirectory;
        Directory.CreateDirectory(dir);
        var baseName = L.Get("status_new_file");
        var name     = $"{baseName} {Files.Count + 1}.md";
        var path     = Path.Combine(dir, name);
        var model    = new FileModel { Name = name, Path = path, Content = string.Empty };
        _fileService.Write(model);
        Files.Add(model);
        SelectedFile = model;
        BeginRenameRequested?.Invoke(model);
    }

    [RelayCommand]
    private void ImportFile(string sourcePath)
    {
        var dir   = _settingsService.Current.MarkdownDirectory;
        var model = _fileService.CopyToAppDirectory(sourcePath, dir);
        Files.Add(model);
        SelectedFile = model;
    }

    [RelayCommand]
    private void DeleteFile()
    {
        if (SelectedFile is null) return;
        if (File.Exists(SelectedFile.Path))
            File.Delete(SelectedFile.Path);
        Files.Remove(SelectedFile);
        SelectedFile = Files.FirstOrDefault();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }

    [RelayCommand]
    private void OpenFileDialog()
    {
        OpenFileRequested?.Invoke();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        OpenSettingsRequested?.Invoke();
    }

    public void RenameFile(FileModel file, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;
        _fileService.Rename(file, newName);
        StatusMessage = file.Name;
    }
}
