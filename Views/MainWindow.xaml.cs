using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Reflection;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Markdig;
using MDOlusturucu.Models;
using MDOlusturucu.Services;
using MDOlusturucu.ViewModels;
using WpfColor      = System.Windows.Media.Color;
using WpfBrush      = System.Windows.Media.Brush;
using WpfBrushes    = System.Windows.Media.Brushes;
using WpfSolidBrush = System.Windows.Media.SolidColorBrush;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfColorConv  = System.Windows.Media.ColorConverter;
using Win32Open     = Microsoft.Win32.OpenFileDialog;
using WpfApp        = System.Windows.Application;

namespace MDOlusturucu.Views;

public partial class MainWindow : Window
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint pt);

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string? lpNewItem);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct NativePoint { public int X; public int Y; }

    private const int  DWMWA_CAPTION_COLOR = 35;
    private const int  DWMWA_TEXT_COLOR    = 36;
    private const int  WM_NCLBUTTONDOWN    = 0x00A1;
    private const int  WM_SYSCOMMAND       = 0x0112;
    private const int  HTSYSMENU           = 3;
    private const uint MF_SEPARATOR        = 0x800;
    private const uint MF_STRING           = 0x0;
    private const uint IDM_SHORTCUT        = 0x9001;

    private bool   _iconDragPending;
    private int    _iconStartX, _iconStartY;

    private readonly MainViewModel    _vm;
    private readonly ISettingsService _settingsService;
    private readonly DispatcherTimer  _saveTimer;
    private FileModel?   _subscribedFile;
    private bool         _isPreviewVisible = true;
    private GridLength   _savedPreviewWidth = new GridLength(1, GridUnitType.Star);

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public MainWindow(MainViewModel viewModel, ISettingsService settingsService)
    {
        InitializeComponent();
        _vm              = viewModel;
        _settingsService = settingsService;
        DataContext      = _vm;

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _saveTimer.Tick += OnSaveTimerTick;

        _vm.PropertyChanged        += OnViewModelPropertyChanged;
        _vm.OpenFileRequested      += ShowOpenFileDialog;
        _vm.OpenSettingsRequested  += ShowSettingsWindow;
        _vm.BeginRenameRequested   += BeginRename;

        SourceInitialized += (_, _) =>
        {
            var hwnd   = new WindowInteropHelper(this).Handle;
            var source = HwndSource.FromHwnd(hwnd);
            source?.AddHook(TitleBarWndProc);
            ApplyTitleBarColors();

            var sysMenu = GetSystemMenu(hwnd, false);
            AppendMenu(sysMenu, MF_SEPARATOR, 0, null);
            AppendMenu(sysMenu, MF_STRING, IDM_SHORTCUT, L.Get("menu_desktop_shortcut"));
        };

        MouseMove += OnIconDragMouseMove;
        PreviewMouseLeftButtonUp += OnIconDragMouseUp;

        ApplyTheme(_vm.IsDarkTheme);
        ApplyEditorSettings();

        _subscribedFile = _vm.SelectedFile;
        if (_subscribedFile is not null)
            _subscribedFile.PropertyChanged += OnSelectedFilePropertyChanged;

        EditorBox.Text = _vm.SelectedFile?.Content ?? string.Empty;
        EditorBox.TextChanged += OnEditorTextChanged;

        RefreshPreview();

        if (!_settingsService.Current.IsPreviewVisible)
            TogglePreview();
    }

    // --- Syntax highlighting ---

    private MarkdownColorizer? _colorizer;

    public void DisableHighlighting()
    {
        if (_colorizer != null)
        {
            EditorBox.TextArea.TextView.LineTransformers.Remove(_colorizer);
            _colorizer = null;
        }
    }

    private void LoadHighlighting(bool dark)
    {
        if (_colorizer != null)
            EditorBox.TextArea.TextView.LineTransformers.Remove(_colorizer);

        _colorizer = new MarkdownColorizer(dark);
        EditorBox.TextArea.TextView.LineTransformers.Add(_colorizer);
    }

    // --- Editör ayarları ---

    private void ApplyEditorSettings()
    {
        var s = _settingsService.Current;

        EditorBox.FontFamily = string.IsNullOrWhiteSpace(s.EditorFontFamily)
            ? new WpfFontFamily("Cascadia Code, Consolas, Courier New")
            : new WpfFontFamily(s.EditorFontFamily);

        if (!string.IsNullOrWhiteSpace(s.EditorFontColor))
        {
            try
            {
                var color = (WpfColor)WpfColorConv.ConvertFromString(s.EditorFontColor)!;
                EditorBox.Foreground = new WpfSolidBrush(color);
            }
            catch { EditorBox.Foreground = WpfBrushes.Transparent; }
        }
        else
        {
            EditorBox.Foreground = (WpfBrush)WpfApp.Current.Resources["PrimaryText"];
        }
    }

    // --- Otomatik kaydetme ---

    private void OnSaveTimerTick(object? sender, EventArgs e)
    {
        _saveTimer.Stop();
        _vm.SaveFileCommand.Execute(null);
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_vm.SelectedFile is null) return;
        if (EditorBox.Text == _vm.SelectedFile.Content) return;

        _vm.SelectedFile.Content = EditorBox.Text;
        _vm.StatusMessage = $"{_vm.SelectedFile.Name}  —  {L.Get("status_unsaved")}";
        _saveTimer.Stop();
        _saveTimer.Start();
        RefreshPreview();
    }

    // --- ViewModel değişiklikleri ---

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.IsDarkTheme):
                ApplyTheme(_vm.IsDarkTheme);
                ApplyEditorSettings();
                RefreshPreview();
                break;

            case nameof(MainViewModel.SelectedFile):
                if (_subscribedFile is not null)
                    _subscribedFile.PropertyChanged -= OnSelectedFilePropertyChanged;

                _subscribedFile = _vm.SelectedFile;

                if (_subscribedFile is not null)
                    _subscribedFile.PropertyChanged += OnSelectedFilePropertyChanged;

                EditorBox.TextChanged -= OnEditorTextChanged;
                EditorBox.Text = _vm.SelectedFile?.Content ?? string.Empty;
                EditorBox.TextChanged += OnEditorTextChanged;

                _saveTimer.Stop();
                RefreshPreview();
                break;

            case nameof(MainViewModel.EditorFontFamily):
                ApplyEditorSettings();
                break;

            case nameof(MainViewModel.EditorFontColor):
                ApplyEditorSettings();
                ApplyTitleBarColors();
                break;
        }
    }

    private void OnSelectedFilePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(FileModel.Content)) return;

        if (EditorBox.Text != _vm.SelectedFile?.Content)
        {
            EditorBox.TextChanged -= OnEditorTextChanged;
            EditorBox.Text = _vm.SelectedFile?.Content ?? string.Empty;
            EditorBox.TextChanged += OnEditorTextChanged;
        }
    }

    // --- Markdown önizleme ---

    private void RefreshPreview()
    {
        var content = _vm.SelectedFile?.Content ?? string.Empty;
        var html    = GeneratePreviewHtml(content, _vm.IsDarkTheme);
        try { PreviewBrowser.NavigateToString(html); }
        catch { }
    }

    private static string GeneratePreviewHtml(string markdown, bool isDark)
    {
        var body = Markdown.ToHtml(markdown, Pipeline);

        var (bg, fg, codeBg, linkColor, borderColor, headBorder, scrollTrack, scrollThumb) = isDark
            ? ("#1E1E2E", "#CDD6F4", "#313244", "#89B4FA", "#45475A", "#585B70", "#181825", "#45475A")
            : ("#FFFFFF",  "#4C4F69", "#EFF1F5", "#1E66F5", "#BCC0CC", "#ACB0BE", "#E6E9EF", "#ACB0BE");

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><meta http-equiv=\"X-UA-Compatible\" content=\"IE=11\"><style>");
        sb.Append("*{box-sizing:border-box;margin:0;padding:0}");
        sb.Append($"body{{background:{bg};color:{fg};font-family:'Segoe UI',system-ui,sans-serif;font-size:14px;line-height:1.75;padding:20px}}");
        sb.Append("h1,h2,h3,h4,h5,h6{margin:1em 0 .4em;font-weight:600}");
        sb.Append($"h1{{font-size:2em;border-bottom:1px solid {headBorder};padding-bottom:.3em}}");
        sb.Append($"h2{{font-size:1.5em;border-bottom:1px solid {headBorder};padding-bottom:.2em}}");
        sb.Append("p{margin:.7em 0}");
        sb.Append($"a{{color:{linkColor};text-decoration:none}}a:hover{{text-decoration:underline}}");
        sb.Append($"code{{background:{codeBg};padding:.15em .4em;border-radius:4px;font-family:'Cascadia Code',Consolas,monospace;font-size:.88em}}");
        sb.Append($"pre{{background:{codeBg};padding:1em;border-radius:8px;overflow-x:auto;margin:.8em 0}}pre code{{background:none;padding:0}}");
        sb.Append($"blockquote{{border-left:3px solid {borderColor};margin:.8em 0;padding:.4em 1em;opacity:.85}}");
        sb.Append("ul,ol{margin:.7em 0 .7em 1.5em}li{margin:.25em 0}");
        sb.Append($"table{{border-collapse:collapse;width:100%;margin:.8em 0}}th,td{{border:1px solid {borderColor};padding:.4em .7em;text-align:left}}th{{background:{codeBg};font-weight:600}}");
        sb.Append($"hr{{border:none;border-top:1px solid {borderColor};margin:1.2em 0}}");
        sb.Append("img{max-width:100%}");
        sb.Append($"html,body{{scrollbar-face-color:{scrollThumb};scrollbar-track-color:{scrollTrack};scrollbar-arrow-color:{scrollTrack};scrollbar-shadow-color:{scrollThumb};scrollbar-highlight-color:{scrollThumb};scrollbar-darkshadow-color:{scrollTrack}}}");
        sb.Append($"::-webkit-scrollbar{{width:8px;height:8px}}::-webkit-scrollbar-track{{background:{scrollTrack}}}::-webkit-scrollbar-thumb{{background:{scrollThumb};border-radius:4px}}");
        sb.Append("</style></head><body>");
        sb.Append(body);
        sb.Append("</body></html>");
        return sb.ToString();
    }

    // --- HTML dışa aktarma ---

    private void ExportHtmlButton_Click(object sender, RoutedEventArgs e) => ExportToHtml();

    private void ExportToHtml()
    {
        if (_vm.SelectedFile is null)
        {
            System.Windows.MessageBox.Show(L.Get("export_no_file"), L.Get("toolbar_export_html"),
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        try
        {
            var htmlPath = Path.ChangeExtension(_vm.SelectedFile.Path, ".html");
            var html     = GenerateExportHtml(_vm.SelectedFile.Content, _vm.IsDarkTheme);
            File.WriteAllText(htmlPath, html, Encoding.UTF8);
            _vm.StatusMessage = $"{L.Get("export_success")} {Path.GetFileName(htmlPath)}";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"{L.Get("export_error")} {ex.Message}", L.Get("toolbar_export_html"),
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private static string GenerateExportHtml(string markdown, bool isDark)
    {
        var body = Markdown.ToHtml(markdown, Pipeline);

        var (bg, fg, codeBg, linkColor, borderColor, headBorder) = isDark
            ? ("#1E1E2E", "#CDD6F4", "#313244", "#89B4FA", "#45475A", "#585B70")
            : ("#EFF1F5", "#4C4F69", "#E6E9EF", "#1E66F5", "#BCC0CC", "#ACB0BE");

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html>");
        sb.Append("<html lang=\"tr\">");
        sb.Append("<head>");
        sb.Append("<meta charset=\"utf-8\">");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.Append("<style>");
        sb.Append("*{box-sizing:border-box;margin:0;padding:0}");
        sb.Append($"body{{background:{bg};color:{fg};font-family:'Segoe UI',system-ui,sans-serif;font-size:16px;line-height:1.8;padding:40px 20px;max-width:860px;margin:0 auto}}");
        sb.Append("h1,h2,h3,h4,h5,h6{margin:1.2em 0 .4em;font-weight:600}");
        sb.Append($"h1{{font-size:2em;border-bottom:1px solid {headBorder};padding-bottom:.3em}}");
        sb.Append($"h2{{font-size:1.5em;border-bottom:1px solid {headBorder};padding-bottom:.2em}}");
        sb.Append("h3{font-size:1.25em}h4{font-size:1.1em}");
        sb.Append("p{margin:.7em 0}");
        sb.Append($"a{{color:{linkColor};text-decoration:none}}a:hover{{text-decoration:underline}}");
        sb.Append($"code{{background:{codeBg};padding:.2em .4em;border-radius:4px;font-family:'Cascadia Code',Consolas,monospace;font-size:.875em}}");
        sb.Append($"pre{{background:{codeBg};padding:1em 1.2em;border-radius:8px;overflow-x:auto;margin:.8em 0}}pre code{{background:none;padding:0}}");
        sb.Append($"blockquote{{border-left:3px solid {borderColor};margin:.8em 0;padding:.4em 1em;opacity:.85}}");
        sb.Append("ul,ol{margin:.7em 0 .7em 1.5em}li{margin:.3em 0}");
        sb.Append($"table{{border-collapse:collapse;width:100%;margin:.8em 0}}th,td{{border:1px solid {borderColor};padding:.5em .8em;text-align:left}}th{{background:{codeBg};font-weight:600}}");
        sb.Append($"hr{{border:none;border-top:1px solid {borderColor};margin:1.5em 0}}");
        sb.Append("img{max-width:100%;height:auto;border-radius:4px}");
        sb.Append($"::-webkit-scrollbar{{width:8px}}::-webkit-scrollbar-track{{background:{bg}}}::-webkit-scrollbar-thumb{{background:{borderColor};border-radius:4px}}");
        sb.Append("</style>");
        sb.Append("</head>");
        sb.Append("<body>");
        sb.Append(body);
        sb.Append("</body></html>");
        return sb.ToString();
    }

    // --- Hakkında penceresi ---

    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow { Owner = this };
        about.ShowDialog();
    }

    // --- Başlık çubuğu ikon sürükleme → masaüstü kısayolu ---

    private IntPtr TitleBarWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_NCLBUTTONDOWN && wParam.ToInt32() == HTSYSMENU)
        {
            _iconStartX      = unchecked((short)(lParam.ToInt32() & 0xFFFF));
            _iconStartY      = unchecked((short)((lParam.ToInt32() >> 16) & 0xFFFF));
            _iconDragPending = true;
            System.Windows.Input.Mouse.Capture(this);
            handled = true;
            return IntPtr.Zero;
        }

        if (msg == WM_SYSCOMMAND && (wParam.ToInt64() & 0xFFF0) == IDM_SHORTCUT)
        {
            CreateDesktopShortcut();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void OnIconDragMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_iconDragPending) return;

        if (GetCursorPos(out NativePoint pt) &&
            (Math.Abs(pt.X - _iconStartX) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(pt.Y - _iconStartY) > SystemParameters.MinimumVerticalDragDistance))
        {
            _iconDragPending = false;
            System.Windows.Input.Mouse.Capture(null);

            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath != null)
            {
                var data = new System.Windows.DataObject(
                    System.Windows.DataFormats.FileDrop, new[] { exePath });
                System.Windows.DragDrop.DoDragDrop(
                    this, data,
                    System.Windows.DragDropEffects.Link | System.Windows.DragDropEffects.Copy);
            }
        }
    }

    private void OnIconDragMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_iconDragPending)
        {
            _iconDragPending = false;
            System.Windows.Input.Mouse.Capture(null);
        }
    }

    private static void CreateDesktopShortcut()
    {
        var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        if (exePath == null) return;

        var desktopPath   = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var shortcutPath  = Path.Combine(desktopPath, "MD Oluşturucu.lnk");
        var shellType     = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null) return;

        dynamic shell    = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath       = exePath;
        shortcut.WorkingDirectory = Path.GetDirectoryName(exePath) ?? "";
        shortcut.IconLocation     = $"{exePath},0";
        shortcut.Save();
        Marshal.ReleaseComObject(shortcut);
        Marshal.ReleaseComObject(shell);
    }

    // --- Önizleme toggle ---

    private void PreviewToggleButton_Click(object sender, RoutedEventArgs e) => TogglePreview();

    private void TogglePreview()
    {
        var splitterCol = EditorPreviewGrid.ColumnDefinitions[1];
        var previewCol  = EditorPreviewGrid.ColumnDefinitions[2];

        if (_isPreviewVisible)
        {
            _savedPreviewWidth     = previewCol.Width;
            previewCol.MinWidth    = 0;
            previewCol.Width       = new GridLength(0);
            splitterCol.Width      = new GridLength(0);
            PreviewPanel.Visibility          = Visibility.Collapsed;
            EditorPreviewSplitter.Visibility = Visibility.Collapsed;
            PreviewToggleButton.Content      = L.Get("toolbar_preview_off");
            _isPreviewVisible = false;
        }
        else
        {
            previewCol.MinWidth    = 200;
            previewCol.Width       = _savedPreviewWidth;
            splitterCol.Width      = new GridLength(5);
            PreviewPanel.Visibility          = Visibility.Visible;
            EditorPreviewSplitter.Visibility = Visibility.Visible;
            PreviewToggleButton.Content      = L.Get("toolbar_preview_on");
            _isPreviewVisible = true;
        }

        var s = _settingsService.Current;
        s.IsPreviewVisible = _isPreviewVisible;
        _settingsService.SaveSilent(s);
    }

    // --- Tema (yalnızca tema sözlüğünü değiştirir, dil sözlüğünü korur) ---

    private void ApplyTheme(bool dark)
    {
        var dict = WpfApp.Current.Resources.MergedDictionaries;

        // Sadece tema sözlüklerini kaldır (Languages/ veya diğerleri korunur)
        for (int i = dict.Count - 1; i >= 0; i--)
        {
            var src = dict[i].Source?.OriginalString ?? "";
            if (src.Contains("Themes/") || src.Contains("Themes\\"))
                dict.RemoveAt(i);
        }

        var uri = dark
            ? new Uri("Themes/Dark.xaml",  UriKind.Relative)
            : new Uri("Themes/Light.xaml", UriKind.Relative);
        dict.Insert(0, new ResourceDictionary { Source = uri });

        EditorBox.Background = dark
            ? new WpfSolidBrush(WpfColor.FromRgb(0x1E, 0x1E, 0x2E))
            : new WpfSolidBrush(WpfColor.FromRgb(0xFF, 0xFF, 0xFF));

        LoadHighlighting(dark);
        ApplyTitleBarColors();
    }

    private void ApplyTitleBarColors()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        var (bgR, bgG, bgB) = _vm.IsDarkTheme
            ? (0x1E, 0x1E, 0x2E)
            : (0xEF, 0xF1, 0xF5);
        int bgColor = bgR | (bgG << 8) | (bgB << 16);
        DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref bgColor, sizeof(int));

        WpfColor textColor;
        var fontColorHex = _settingsService.Current.EditorFontColor;
        if (!string.IsNullOrWhiteSpace(fontColorHex))
        {
            try   { textColor = (WpfColor)WpfColorConv.ConvertFromString(fontColorHex)!; }
            catch { textColor = DefaultTitleTextColor(); }
        }
        else
        {
            textColor = DefaultTitleTextColor();
        }
        int fgColor = textColor.R | (textColor.G << 8) | (textColor.B << 16);
        DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref fgColor, sizeof(int));
    }

    private WpfColor DefaultTitleTextColor() => _vm.IsDarkTheme
        ? WpfColor.FromRgb(0xCD, 0xD6, 0xF4)
        : WpfColor.FromRgb(0x4C, 0x4F, 0x69);

    // --- Dosya açma ---

    private void ShowOpenFileDialog()
    {
        var dialog = new Win32Open
        {
            Filter = L.Get("dialog_filter_md"),
            Title  = L.Get("dialog_title_open")
        };
        if (dialog.ShowDialog() == true)
            _vm.ImportFileCommand.Execute(dialog.FileName);
    }

    // --- Ayarlar penceresi ---

    private void ShowSettingsWindow()
    {
        var settingsVm = new SettingsViewModel(_settingsService);
        var window     = new SettingsWindow(settingsVm) { Owner = this };
        window.ShowDialog();
        ApplyEditorSettings();
    }

    // --- Dosya adı düzenleme ---

    private void BeginRename(FileModel file)
    {
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            var box = FindFileNameBox(file);
            if (box is null) return;
            ActivateRenameBox(box);
        });
    }

    private void FileNameBlock_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount < 2) return;
        e.Handled = true;
        if (sender is System.Windows.Controls.TextBlock tb &&
            tb.Parent is System.Windows.Controls.Grid grid)
        {
            var box = grid.Children.OfType<System.Windows.Controls.TextBox>()
                          .FirstOrDefault(x => x.Name == "FileNameBox");
            if (box is not null) ActivateRenameBox(box);
        }
    }

    private static void ActivateRenameBox(System.Windows.Controls.TextBox box)
    {
        var block = (box.Parent as System.Windows.Controls.Grid)?
            .Children.OfType<System.Windows.Controls.TextBlock>()
            .FirstOrDefault(x => x.Name == "FileNameBlock");

        if (block is not null) block.Visibility = Visibility.Collapsed;
        box.Visibility = Visibility.Visible;
        box.SelectAll();
        box.Focus();
    }

    private void FileNameBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox box) return;

        if (e.Key == System.Windows.Input.Key.Enter)
        {
            CommitRename(box);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Escape)
        {
            CancelRename(box);
            e.Handled = true;
        }
    }

    private void FileNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox box)
            CommitRename(box);
    }

    private void CommitRename(System.Windows.Controls.TextBox box)
    {
        if (box.DataContext is FileModel file)
            _vm.RenameFile(file, box.Text);
        RestoreBlock(box);
    }

    private static void CancelRename(System.Windows.Controls.TextBox box)
    {
        if (box.DataContext is FileModel file)
            box.Text = file.Name;
        RestoreBlock(box);
    }

    private static void RestoreBlock(System.Windows.Controls.TextBox box)
    {
        box.Visibility = Visibility.Collapsed;
        var block = (box.Parent as System.Windows.Controls.Grid)?
            .Children.OfType<System.Windows.Controls.TextBlock>()
            .FirstOrDefault(x => x.Name == "FileNameBlock");
        if (block is not null) block.Visibility = Visibility.Visible;
    }

    private System.Windows.Controls.TextBox? FindFileNameBox(FileModel file)
    {
        for (int i = 0; i < FileListBox.Items.Count; i++)
        {
            var container = FileListBox.ItemContainerGenerator
                .ContainerFromIndex(i) as System.Windows.Controls.ListBoxItem;
            if (container?.DataContext != file) continue;

            var grid = FindVisualChild<System.Windows.Controls.Grid>(container);
            return grid?.Children.OfType<System.Windows.Controls.TextBox>()
                        .FirstOrDefault(x => x.Name == "FileNameBox");
        }
        return null;
    }

    private static T? FindVisualChild<T>(System.Windows.DependencyObject parent)
        where T : System.Windows.DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var result = FindVisualChild<T>(child);
            if (result is not null) return result;
        }
        return null;
    }
}
