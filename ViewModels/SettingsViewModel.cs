using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MDOlusturucu.Models;
using MDOlusturucu.Services;

namespace MDOlusturucu.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string? _markdownDirectory;

    [ObservableProperty]
    private string? _selectedFontFamily;

    [ObservableProperty]
    private string? _selectedFontColor;

    [ObservableProperty]
    private LanguageItem? _selectedLanguage;

    public List<string> AvailableFonts { get; } = Fonts.SystemFontFamilies
        .Select(f => f.Source)
        .OrderBy(s => s)
        .ToList();

    public List<ColorOption> AvailableColors { get; } =
    [
        new(L.Get("color_default"),     ""),
        new(L.Get("color_white"),       "#FFFFFF"),
        new(L.Get("color_light_gray"),  "#CDD6F4"),
        new(L.Get("color_yellow"),      "#F9E2AF"),
        new(L.Get("color_orange"),      "#FAB387"),
        new(L.Get("color_red"),         "#F38BA8"),
        new(L.Get("color_pink"),        "#F5C2E7"),
        new(L.Get("color_green"),       "#A6E3A1"),
        new(L.Get("color_blue"),        "#89B4FA"),
        new(L.Get("color_light_blue"),  "#89DCEB"),
        new(L.Get("color_purple"),      "#CBA6F7"),
        new(L.Get("color_black"),       "#1E1E2E"),
        new(L.Get("color_dark_gray"),   "#4C4F69"),
    ];

    public List<LanguageItem> AvailableLanguages { get; } =
    [
        new("tr", "Türkçe"),
        new("en", "English"),
        new("fr", "Français"),
        new("de", "Deutsch"),
        new("es", "Español"),
        new("ru", "Русский"),
    ];

    public event Action? CloseRequested;
    public event Action? BrowseFolderRequested;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService    = settingsService;
        _markdownDirectory  = settingsService.Current.MarkdownDirectory;
        _selectedFontFamily = settingsService.Current.EditorFontFamily;
        _selectedFontColor  = settingsService.Current.EditorFontColor ?? "";

        var currentLang = settingsService.Current.Language ?? "tr";
        _selectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == currentLang)
                            ?? AvailableLanguages[0];
    }

    [RelayCommand]
    private void BrowseFolder()
    {
        BrowseFolderRequested?.Invoke();
    }

    [RelayCommand]
    private void Save()
    {
        var prevLang = _settingsService.Current.Language ?? "tr";
        var newLang  = SelectedLanguage?.Code ?? "tr";

        _settingsService.Save(new AppSettings
        {
            MarkdownDirectory = string.IsNullOrWhiteSpace(MarkdownDirectory)
                ? _settingsService.Current.MarkdownDirectory
                : MarkdownDirectory,
            EditorFontFamily  = string.IsNullOrWhiteSpace(SelectedFontFamily)
                ? "Cascadia Code"
                : SelectedFontFamily,
            EditorFontColor   = SelectedFontColor ?? "",
            IsPreviewVisible  = _settingsService.Current.IsPreviewVisible,
            Language          = newLang
        });

        if (newLang != prevLang)
        {
            System.Windows.MessageBox.Show(
                L.Get("restart_required"),
                L.Get("settings_title"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke();
    }
}

public record ColorOption(string Name, string Hex);
public record LanguageItem(string Code, string DisplayName);
