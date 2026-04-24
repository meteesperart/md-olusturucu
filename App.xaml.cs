using MDOlusturucu.Services;
using MDOlusturucu.ViewModels;
using MDOlusturucu.Views;

namespace MDOlusturucu;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, ex) =>
        {
            ex.Handled = true;

            // Highlighting regex hatası uygulamayı çökertmesin
            if (ex.Exception.Message.Contains("matched 0 characters") ||
                ex.Exception.Message.Contains("highlighting rule"))
            {
                if (Current.MainWindow is MDOlusturucu.Views.MainWindow mw)
                    mw.DisableHighlighting();
                try
                {
                    var log = System.IO.Path.Combine(AppContext.BaseDirectory, "highlight_error.log");
                    System.IO.File.WriteAllText(log, ex.Exception.Message + "\n\nStack:\n" + ex.Exception.StackTrace);
                }
                catch { }
                return;
            }

            try
            {
                var log = System.IO.Path.Combine(AppContext.BaseDirectory, "error.log");
                using var fs = new System.IO.FileStream(log,
                    System.IO.FileMode.Append, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite);
                using var sw = new System.IO.StreamWriter(fs);
                sw.WriteLine($"[{DateTime.Now:HH:mm:ss}] {ex.Exception.GetType().Name}: {ex.Exception.Message}");
                sw.WriteLine(ex.Exception.StackTrace);
                sw.WriteLine();
            }
            catch { }
        };

        try
        {
            var settingsService = new SettingsService();
            var fileService     = new FileService();

            // Dil sözlüğünü yükle (App.xaml'daki tema sözlüğü zaten yüklü)
            LoadLanguageDictionary(settingsService.Current.Language);

            var vm     = new MainViewModel(fileService, settingsService);
            var window = new MainWindow(vm, settingsService);
            window.Show();
        }
        catch (Exception ex)
        {
            var log = System.IO.Path.Combine(AppContext.BaseDirectory, "startup_error.log");
            System.IO.File.WriteAllText(log, ex.ToString());
            System.Windows.MessageBox.Show(ex.Message, "Başlatma Hatası");
        }
    }

    private static void LoadLanguageDictionary(string langCode)
    {
        var validCodes = new[] { "tr", "en", "fr", "de", "es", "ru" };
        if (!System.Array.Exists(validCodes, c => c == langCode))
            langCode = "tr";

        try
        {
            var uri  = new Uri($"Languages/Strings.{langCode}.xaml", UriKind.Relative);
            var dict = new System.Windows.ResourceDictionary { Source = uri };
            Current.Resources.MergedDictionaries.Add(dict);
        }
        catch
        {
            // Dil dosyası yüklenemezse Türkçe'ye geri dön
            var uri  = new Uri("Languages/Strings.tr.xaml", UriKind.Relative);
            var dict = new System.Windows.ResourceDictionary { Source = uri };
            Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}
