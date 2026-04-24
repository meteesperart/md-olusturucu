namespace MDOlusturucu.Models;

public class AppSettings
{
    public string MarkdownDirectory { get; set; } = string.Empty;
    public string EditorFontFamily  { get; set; } = "Cascadia Code";
    public string EditorFontColor   { get; set; } = "";
    public bool   IsPreviewVisible  { get; set; } = true;
    public string Language          { get; set; } = "tr";
}
