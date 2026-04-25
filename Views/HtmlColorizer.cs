using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using WpfColor = System.Windows.Media.Color;
using WpfBrush = System.Windows.Media.SolidColorBrush;

namespace MDOlusturucu.Views;

public class HtmlColorizer : DocumentColorizingTransformer
{
    private readonly bool _dark;

    public HtmlColorizer(bool dark) => _dark = dark;

    private WpfBrush B(int rgb) => new(WpfColor.FromRgb(
        (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));

    private WpfBrush TagBrush     => B(_dark ? 0x89B4FA : 0x1E66F5); // Mavi   – etiket adı ve <> />
    private WpfBrush AttrBrush    => B(_dark ? 0xF38BA8 : 0xE6465E); // Pembe  – öznitelik adı
    private WpfBrush ValueBrush   => B(_dark ? 0x89DCEB : 0x209FB5); // Açık mavi – öznitelik değeri
    private WpfBrush CommentBrush => B(_dark ? 0x6C7086 : 0x8C8FA1); // Gri    – yorum
    private WpfBrush DoctypeBrush => B(_dark ? 0xCBA6F7 : 0x8839EF); // Mor    – DOCTYPE

    // -----------------------------------------------------------------------
    protected override void ColorizeLine(DocumentLine line)
    {
        var doc  = CurrentContext.Document;
        var text = doc.GetText(line.Offset, line.Length);
        if (text.Length == 0) return;

        int baseOffset = line.Offset;
        bool inComment = IsLineInComment(line);

        int i = 0;
        while (i < text.Length)
        {
            if (inComment)
            {
                // Yorum kapanışını ara
                int end = text.IndexOf("-->", i, StringComparison.Ordinal);
                if (end < 0) { Paint(baseOffset + i, text.Length - i, CommentBrush); return; }
                Paint(baseOffset + i, end + 3 - i, CommentBrush);
                i = end + 3;
                inComment = false;
            }
            else if (Eq(text, i, "<!--"))
            {
                int end = text.IndexOf("-->", i + 4, StringComparison.Ordinal);
                if (end < 0) { Paint(baseOffset + i, text.Length - i, CommentBrush); return; }
                Paint(baseOffset + i, end + 3 - i, CommentBrush);
                i = end + 3;
            }
            else if (EqI(text, i, "<!DOCTYPE"))
            {
                int end = text.IndexOf('>', i);
                int len = end < 0 ? text.Length - i : end - i + 1;
                Paint(baseOffset + i, len, DoctypeBrush);
                i += len;
            }
            else if (text[i] == '<')
            {
                i = ColorizeTag(text, baseOffset, i);
            }
            else
            {
                i++;
            }
        }
    }

    // -----------------------------------------------------------------------
    private int ColorizeTag(string text, int baseOffset, int start)
    {
        int i = start;         // '<' konumunda
        int tagNameStart = i;
        i++;                   // '<' atla
        if (i < text.Length && text[i] == '/') i++;   // kapanış etiketindeki '/'

        // Etiket adını oku
        int nameStart = i;
        while (i < text.Length && IsTagNameChar(text[i])) i++;

        if (i == nameStart) return start + 1; // '<' sonrasında isim yok

        // '<', opsiyonel '/', etiket adını boyar
        Paint(baseOffset + tagNameStart, i - tagNameStart, TagBrush);

        // Öznitelikleri işle
        while (i < text.Length && text[i] != '>')
        {
            // Boşluk atla
            while (i < text.Length && IsWS(text[i])) i++;
            if (i >= text.Length || text[i] == '>' || text[i] == '/') break;

            // Öznitelik adı
            int attrStart = i;
            while (i < text.Length && text[i] != '=' && text[i] != '>' && !IsWS(text[i]) && text[i] != '/') i++;
            if (i > attrStart) Paint(baseOffset + attrStart, i - attrStart, AttrBrush);

            // Boşluk atla
            while (i < text.Length && IsWS(text[i])) i++;

            // Değer kısmı
            if (i < text.Length && text[i] == '=')
            {
                i++; // '=' atla
                while (i < text.Length && IsWS(text[i])) i++;

                if (i < text.Length && (text[i] == '"' || text[i] == '\''))
                {
                    char q = text[i];
                    int valStart = i;
                    i++;
                    while (i < text.Length && text[i] != q) i++;
                    if (i < text.Length) i++; // kapanış tırnağı
                    Paint(baseOffset + valStart, i - valStart, ValueBrush);
                }
                else
                {
                    int valStart = i;
                    while (i < text.Length && text[i] != '>' && !IsWS(text[i])) i++;
                    if (i > valStart) Paint(baseOffset + valStart, i - valStart, ValueBrush);
                }
            }
        }

        // '>' veya '/>' boyar
        if (i < text.Length && text[i] == '/') { Paint(baseOffset + i, 1, TagBrush); i++; }
        if (i < text.Length && text[i] == '>') { Paint(baseOffset + i, 1, TagBrush); i++; }

        return i;
    }

    // -----------------------------------------------------------------------
    // Önceki satırları tarayarak bu satırın yorum içinde başlayıp başlamadığını döner
    private bool IsLineInComment(DocumentLine line)
    {
        var doc = CurrentContext.Document;
        bool inside = false;
        for (int ln = 1; ln < line.LineNumber; ln++)
        {
            var l = doc.GetLineByNumber(ln);
            var t = doc.GetText(l.Offset, l.Length);
            int i = 0;
            while (i < t.Length)
            {
                if (!inside && Eq(t, i, "<!--")) { inside = true;  i += 4; }
                else if (inside && Eq(t, i, "-->")) { inside = false; i += 3; }
                else i++;
            }
        }
        return inside;
    }

    // -----------------------------------------------------------------------
    private static bool IsTagNameChar(char c) =>
        char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == ':' || c == '.';

    private static bool IsWS(char c) => c == ' ' || c == '\t';

    private static bool Eq(string text, int i, string s)
    {
        if (i + s.Length > text.Length) return false;
        for (int k = 0; k < s.Length; k++)
            if (text[i + k] != s[k]) return false;
        return true;
    }

    private static bool EqI(string text, int i, string s)
    {
        if (i + s.Length > text.Length) return false;
        for (int k = 0; k < s.Length; k++)
            if (char.ToUpperInvariant(text[i + k]) != char.ToUpperInvariant(s[k])) return false;
        return true;
    }

    private void Paint(int start, int length, WpfBrush brush)
    {
        if (length <= 0) return;
        ChangeLinePart(start, start + length, el =>
            el.TextRunProperties.SetForegroundBrush(brush));
    }
}
