using System.IO;
using System.Reflection;
using System.Windows;
using Markdig;

namespace MDOlusturucu.Views;

public partial class ChangelogWindow : Window
{
    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public ChangelogWindow(bool isDark)
    {
        InitializeComponent();
        LoadChangelog(isDark);
    }

    private void LoadChangelog(bool isDark)
    {
        string md;
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames()
                      .FirstOrDefault(n => n.EndsWith("CHANGELOG.md", StringComparison.OrdinalIgnoreCase));
        if (name != null)
        {
            using var stream = asm.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            md = reader.ReadToEnd();
        }
        else
        {
            md = "CHANGELOG.md bulunamadı.";
        }

        var body    = isDark ? "#1E1E2E" : "#EFF1F5";
        var text    = isDark ? "#CDD6F4" : "#4C4F69";
        var heading = isDark ? "#89B4FA" : "#1E66F5";
        var muted   = isDark ? "#6C7086" : "#8C8FA1";
        var border  = isDark ? "#313244" : "#BCC0CC";
        var accent  = isDark ? "#A6E3A1" : "#40A02B";

        var htmlBody = Markdown.ToHtml(md, Pipeline);
        var html = $@"<!DOCTYPE html>
<html>
<head>
<meta http-equiv=""X-UA-Compatible"" content=""IE=11"">
<meta charset=""utf-8"">
<style>
  body   {{ background:{body}; color:{text}; font-family:'Segoe UI',system-ui,sans-serif;
            font-size:14px; line-height:1.7; padding:20px 24px; margin:0; }}
  h1     {{ color:{heading}; font-size:1.5em; border-bottom:2px solid {border}; padding-bottom:.4em; }}
  h2     {{ color:{heading}; font-size:1.1em; margin-top:1.6em; border-bottom:1px solid {border}; padding-bottom:.2em; }}
  ul     {{ margin:.4em 0; padding-left:1.4em; }}
  li     {{ margin:.25em 0; }}
  li::marker {{ color:{accent}; }}
  em     {{ color:{muted}; font-style:normal; font-size:.9em; }}
</style>
</head>
<body>{htmlBody}</body>
</html>";

        ChangelogBrowser.NavigateToString(html);
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();
}
