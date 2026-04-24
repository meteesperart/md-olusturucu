using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;
using WpfBrush = System.Windows.Media.SolidColorBrush;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace MDOlusturucu.Views;

public class MarkdownColorizer : DocumentColorizingTransformer
{
    private readonly bool _dark;

    public MarkdownColorizer(bool dark) => _dark = dark;

    private WpfBrush B(int rgb) => new(WpfColor.FromRgb(
        (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));

    private WpfBrush H1Brush    => B(_dark ? 0x89B4FA : 0x1E66F5);
    private WpfBrush H2Brush    => B(_dark ? 0x74C7EC : 0x04A5E5);
    private WpfBrush H3Brush    => B(_dark ? 0x89DCEB : 0x179299);
    private WpfBrush H456Brush  => B(_dark ? 0x89DCEB : 0x179299);
    private WpfBrush BoldBrush  => B(_dark ? 0xF9E2AF : 0xDF8E1D);
    private WpfBrush ItalicBrush => B(_dark ? 0xF5C2E7 : 0xEA76CB);
    private WpfBrush BIBrush    => B(_dark ? 0xFAB387 : 0xFE640B);
    private WpfBrush StrikeBrush => B(_dark ? 0x585B70 : 0x9CA0B0);
    private WpfBrush CodeBrush  => B(_dark ? 0xA6E3A1 : 0x40A02B);
    private WpfBrush LinkBrush  => B(_dark ? 0x89DCEB : 0x1E66F5);
    private WpfBrush ImageBrush => B(_dark ? 0xCBA6F7 : 0x8839EF);
    private WpfBrush QuoteBrush => B(_dark ? 0x9399B2 : 0x7C7F93);
    private WpfBrush HRuleBrush => B(_dark ? 0x45475A : 0xBCC0CC);
    private WpfBrush ListBrush  => B(_dark ? 0xFAB387 : 0xFE640B);
    private WpfBrush HtmlBrush  => B(_dark ? 0xF38BA8 : 0xD20F39);

    protected override void ColorizeLine(DocumentLine line)
    {
        var doc = CurrentContext.Document;
        var t   = doc.GetText(line.Offset, line.Length);
        int o   = line.Offset;

        if (t.Length == 0) return;

        // Kod bloğu içindeyse tümünü code rengi ver
        if (IsInCodeBlock(line) || t.TrimStart().StartsWith("```"))
        {
            Paint(o, line.Length, CodeBrush);
            return;
        }

        // Başlıklar
        if      (t.StartsWith("###### ")) { Paint(o, line.Length, H456Brush, bold: true); return; }
        else if (t.StartsWith("##### "))  { Paint(o, line.Length, H456Brush, bold: true); return; }
        else if (t.StartsWith("#### "))   { Paint(o, line.Length, H456Brush, bold: true); return; }
        else if (t.StartsWith("### "))    { Paint(o, line.Length, H3Brush,   bold: true); return; }
        else if (t.StartsWith("## "))     { Paint(o, line.Length, H2Brush,   bold: true); return; }
        else if (t.StartsWith("# "))      { Paint(o, line.Length, H1Brush,   bold: true); return; }

        // Alıntı
        if (t.StartsWith(">")) { Paint(o, line.Length, QuoteBrush, italic: true); return; }

        // Yatay çizgi
        if (Regex.IsMatch(t, @"^---+\s*$") || Regex.IsMatch(t, @"^\*\*\*+\s*$"))
        { Paint(o, line.Length, HRuleBrush); return; }

        // Satır içi öğeler
        foreach (Match m in Regex.Matches(t, @"`[^`\r\n]+`"))
            Paint(o + m.Index, m.Length, CodeBrush);

        foreach (Match m in Regex.Matches(t, @"\*\*\*\S.*?\*\*\*"))
            Paint(o + m.Index, m.Length, BIBrush, bold: true, italic: true);
        foreach (Match m in Regex.Matches(t, @"___\S.*?___"))
            Paint(o + m.Index, m.Length, BIBrush, bold: true, italic: true);

        foreach (Match m in Regex.Matches(t, @"\*\*\S.*?\*\*"))
            Paint(o + m.Index, m.Length, BoldBrush, bold: true);
        foreach (Match m in Regex.Matches(t, @"__\S.*?__"))
            Paint(o + m.Index, m.Length, BoldBrush, bold: true);

        foreach (Match m in Regex.Matches(t, @"\*\S.*?\*"))
            Paint(o + m.Index, m.Length, ItalicBrush, italic: true);
        foreach (Match m in Regex.Matches(t, @"_\S.*?_"))
            Paint(o + m.Index, m.Length, ItalicBrush, italic: true);

        foreach (Match m in Regex.Matches(t, @"~~[^~\r\n]+~~"))
            Paint(o + m.Index, m.Length, StrikeBrush);

        foreach (Match m in Regex.Matches(t, @"!\[[^\]]*\]\([^)]*\)"))
            Paint(o + m.Index, m.Length, ImageBrush);

        foreach (Match m in Regex.Matches(t, @"\[[^\]]+\]\([^)]*\)"))
            Paint(o + m.Index, m.Length, LinkBrush);

        var lm = Regex.Match(t, @"^\s*[-*+] |^\s*\d+\. ");
        if (lm.Success) Paint(o + lm.Index, lm.Length, ListBrush);

        foreach (Match m in Regex.Matches(t, @"</?[a-zA-Z][^>]*/?>"))
            Paint(o + m.Index, m.Length, HtmlBrush);
    }

    private bool IsInCodeBlock(DocumentLine line)
    {
        var doc = CurrentContext.Document;
        int count = 0;
        for (int i = 1; i < line.LineNumber; i++)
        {
            var l = doc.GetLineByNumber(i);
            var t = doc.GetText(l.Offset, l.Length);
            if (t.TrimStart().StartsWith("```")) count++;
        }
        return count % 2 == 1;
    }

    private void Paint(int start, int length, WpfBrush brush,
                       bool bold = false, bool italic = false)
    {
        if (length <= 0) return;
        ChangeLinePart(start, start + length, el =>
        {
            el.TextRunProperties.SetForegroundBrush(brush);
            if (bold || italic)
            {
                var tf = el.TextRunProperties.Typeface;
                el.TextRunProperties.SetTypeface(new Typeface(
                    tf.FontFamily,
                    italic ? FontStyles.Italic  : FontStyles.Normal,
                    bold   ? FontWeights.Bold   : FontWeights.Normal,
                    tf.Stretch));
            }
        });
    }
}
