using System.Reflection;
using System.Windows;

namespace MDOlusturucu.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "1.0";

        VersionText.Text = version;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) => Close();
}
