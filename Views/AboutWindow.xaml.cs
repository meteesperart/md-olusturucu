using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace MDOlusturucu.Views;

public partial class AboutWindow : Window
{
    private readonly bool _isDark;

    public AboutWindow(bool isDark)
    {
        InitializeComponent();
        _isDark = isDark;

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "2.0";

        VersionText.Text = version;
    }

    private void ChangelogLink_Click(object sender, RoutedEventArgs e)
    {
        var win = new ChangelogWindow(_isDark) { Owner = this };
        win.ShowDialog();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) => Close();
}
