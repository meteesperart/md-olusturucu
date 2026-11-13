using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Navigation;
using System.Windows.Threading;
using MDOlusturucu.Services;
using MDOlusturucu.ViewModels;
using WpfApp = System.Windows.Application;

namespace MDOlusturucu.Views;

/// <summary>
/// Salt okunur .md görüntüleyici. Dosyayı bulunduğu yerden okur;
/// MD klasörüne kopyalamaz, hiçbir şey yazmaz.
/// </summary>
public partial class ViewerWindow : Window
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int  DWMWA_CAPTION_COLOR = 35;
    private const int  DWMWA_TEXT_COLOR    = 36;
    private const long MaxInlineImageBytes = 10 * 1024 * 1024;

    private static readonly Regex ImgSrcRegex = new(
        "(<img\\b[^>]*?\\bsrc\\s*=\\s*)([\"'])(.*?)\\2",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ISettingsService _settingsService;
    private readonly DispatcherTimer  _reloadTimer;
    private FileSystemWatcher? _watcher;
    private System.Windows.Controls.ContextMenu? _contextMenu;
    private string _filePath;
    private bool   _isDark = true;

    public ViewerWindow(string filePath, ISettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _filePath        = Path.GetFullPath(filePath);

        // Dosya dışarıda değişirse görünümü yenile (kaydetme sırasındaki art arda olayları birleştir)
        _reloadTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _reloadTimer.Tick += (_, _) => { _reloadTimer.Stop(); Render(); };

        SourceInitialized += (_, _) => ApplyTitleBarColors();
        Closed            += (_, _) => _watcher?.Dispose();

        ViewerBrowser.ObjectForScripting = new ViewerScriptHost(this);
        ViewerBrowser.Navigating        += OnNavigating;

        LoadFile(_filePath);
    }

    // --- Dosya yükleme ---

    private void LoadFile(string path)
    {
        _filePath         = path;
        Title             = $"{Path.GetFileName(path)} — MD Oluşturucu";
        FileNameText.Text = Path.GetFileName(path);
        FilePathText.Text = path;
        WatchFile(path);
        Render();
    }

    private void Render()
    {
        string html;
        try
        {
            var html0 = MainWindow.GeneratePreviewHtml(ReadShared(_filePath), _isDark);
            html = InlineLocalImages(html0, Path.GetDirectoryName(_filePath) ?? "");
        }
        catch (Exception ex)
        {
            var msg = System.Net.WebUtility.HtmlEncode($"{L.Get("viewer_read_error")}: {ex.Message}");
            html = MainWindow.GeneratePreviewHtml(string.Empty, _isDark)
                .Replace("<body>", $"<body><p>{msg}</p>");
        }

        try { ViewerBrowser.NavigateToString(html); } catch { }
    }

    // Başka bir program dosyayı yazarken de okuyabilmek için paylaşımlı aç
    private static string ReadShared(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var sr = new StreamReader(fs);
        return sr.ReadToEnd();
    }

    private void WatchFile(string path)
    {
        _watcher?.Dispose();
        _watcher = null;
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir)) return;

            _watcher = new FileSystemWatcher(dir, Path.GetFileName(path))
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size |
                               NotifyFilters.FileName  | NotifyFilters.CreationTime
            };
            FileSystemEventHandler onChange = (_, _) => Dispatcher.BeginInvoke(() =>
            {
                _reloadTimer.Stop();
                _reloadTimer.Start();
            });
            _watcher.Changed += onChange;
            _watcher.Created += onChange;
            _watcher.Renamed += (s, e) => onChange(s, e);
            _watcher.EnableRaisingEvents = true;
        }
        catch { /* izleme kurulamazsa (ağ yolu vb.) görüntüleme yine çalışır */ }
    }

    // --- Göreli resimler ---

    // NavigateToString ile yüklenen sayfanın adresi about:blank olduğundan göreli
    // resim yolları çözülemez; yerel resimleri sayfaya gömerek gösteriyoruz.
    private static string InlineLocalImages(string html, string baseDir)
    {
        return ImgSrcRegex.Replace(html, m =>
        {
            try
            {
                var src = System.Net.WebUtility.HtmlDecode(m.Groups[3].Value);
                if (string.IsNullOrWhiteSpace(src) ||
                    src.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
                    src.StartsWith("http:", StringComparison.OrdinalIgnoreCase) ||
                    src.StartsWith("https:", StringComparison.OrdinalIgnoreCase) ||
                    src.StartsWith("//"))
                    return m.Value;

                string local;
                if (src.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                {
                    local = new Uri(src).LocalPath;
                }
                else
                {
                    var cut = src.IndexOfAny(['?', '#']);
                    if (cut >= 0) src = src[..cut];
                    local = Path.GetFullPath(Path.Combine(baseDir, Uri.UnescapeDataString(src)));
                }

                var mime = Path.GetExtension(local).ToLowerInvariant() switch
                {
                    ".png"            => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".gif"            => "image/gif",
                    ".bmp"            => "image/bmp",
                    ".svg"            => "image/svg+xml",
                    ".ico"            => "image/x-icon",
                    _                 => null
                };
                if (mime is null) return m.Value;

                var info = new FileInfo(local);
                if (!info.Exists || info.Length > MaxInlineImageBytes) return m.Value;

                var data = Convert.ToBase64String(File.ReadAllBytes(local));
                return $"{m.Groups[1].Value}\"data:{mime};base64,{data}\"";
            }
            catch
            {
                return m.Value;
            }
        });
    }

    // --- Bağlantılar ---

    private void OnNavigating(object sender, NavigatingCancelEventArgs e)
    {
        // NavigateToString'in kendi yüklemesinde Uri boştur
        if (e.Uri is null) return;

        try
        {
            var uri = e.Uri;

            if (uri.Scheme == "about")
            {
                // Sayfa içi bağlantı (#baslik)
                var rel = uri.OriginalString["about:".Length..];
                if (rel.StartsWith("blank", StringComparison.OrdinalIgnoreCase)) return;

                // Göreli bağlantı: başka bir .md ise aynı pencerede aç
                e.Cancel = true;
                var cut = rel.IndexOfAny(['?', '#']);
                if (cut >= 0) rel = rel[..cut];
                var dir    = Path.GetDirectoryName(_filePath) ?? "";
                var target = Path.GetFullPath(Path.Combine(dir, Uri.UnescapeDataString(rel)));
                OpenLocalTarget(target);
                return;
            }

            e.Cancel = true;

            if (uri.Scheme is "http" or "https" or "mailto")
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            else if (uri.IsFile)
                OpenLocalTarget(uri.LocalPath);
        }
        catch
        {
            e.Cancel = true;
        }
    }

    private void OpenLocalTarget(string target)
    {
        if (!File.Exists(target)) return;
        if (target.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            Dispatcher.BeginInvoke(() => LoadFile(target));
    }

    // --- Düğmeler ---

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        _isDark = !_isDark;

        var dict = WpfApp.Current.Resources.MergedDictionaries;
        for (int i = dict.Count - 1; i >= 0; i--)
        {
            var src = dict[i].Source?.OriginalString ?? "";
            if (src.Contains("Themes/") || src.Contains("Themes\\"))
                dict.RemoveAt(i);
        }
        var uri = _isDark
            ? new Uri("Themes/Dark.xaml",  UriKind.Relative)
            : new Uri("Themes/Light.xaml", UriKind.Relative);
        dict.Insert(0, new ResourceDictionary { Source = uri });

        ApplyTitleBarColors();
        Render();
    }

    // Tam düzenleyiciye geç. Düzenleyici kendi MD klasöründe çalıştığı için
    // dosya ancak burada, kullanıcı isteyince içe aktarılır.
    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var vm     = new MainViewModel(new FileService(), _settingsService);
            var window = new MainWindow(vm, _settingsService);
            WpfApp.Current.MainWindow = window;
            window.Show();

            var path = _filePath;
            window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                () => vm.ImportFileCommand.Execute(path));

            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "MD Oluşturucu");
        }
    }

    // --- Sağ tık menüsü ---

    public void ShowContextMenu()
    {
        if (_contextMenu is null)
        {
            _contextMenu = new System.Windows.Controls.ContextMenu();
            _contextMenu.Items.Add(MenuItem("menu_copy",       "copy"));
            _contextMenu.Items.Add(MenuItem("menu_select_all", "selectAll"));
        }
        _contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        _contextMenu.IsOpen    = true;
    }

    private System.Windows.Controls.MenuItem MenuItem(string key, string cmd)
    {
        var mi = new System.Windows.Controls.MenuItem { Header = L.Get(key) };
        mi.Click += (_, _) =>
        {
            try { ViewerBrowser.InvokeScript("eval", new object[] { $"document.execCommand('{cmd}')" }); } catch { }
        };
        return mi;
    }

    // --- Başlık çubuğu rengi ---

    private void ApplyTitleBarColors()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        var (bgR, bgG, bgB) = _isDark ? (0x1E, 0x1E, 0x2E) : (0xEF, 0xF1, 0xF5);
        var (fgR, fgG, fgB) = _isDark ? (0xCD, 0xD6, 0xF4) : (0x4C, 0x4F, 0x69);
        int bg = bgR | (bgG << 8) | (bgB << 16);
        int fg = fgR | (fgG << 8) | (fgB << 16);
        DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref bg, sizeof(int));
        DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR,    ref fg, sizeof(int));
    }
}

[ComVisible(true)]
public sealed class ViewerScriptHost
{
    private readonly ViewerWindow _w;
    public ViewerScriptHost(ViewerWindow w) => _w = w;
    public void ShowContextMenu() =>
        _w.Dispatcher.BeginInvoke((Action)_w.ShowContextMenu);
}
