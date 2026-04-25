using CommunityToolkit.Mvvm.ComponentModel;

namespace MDOlusturucu.Models;

public partial class FileModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _path = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    public bool IsHtml => System.IO.Path.GetExtension(Path)
        .Equals(".html", StringComparison.OrdinalIgnoreCase);
}
