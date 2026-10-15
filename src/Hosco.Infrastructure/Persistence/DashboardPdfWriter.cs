using System.Globalization;
using System.Text;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace Hosco.Infrastructure.Persistence;

internal static class DashboardPdfWriter
{
    // Initialized once before this application creates any PDFsharp font.
    private static readonly Lazy<XFont> Font = new(() =>
    {
        GlobalFontSettings.FontResolver ??= new DashboardFontResolver();
        return new XFont("HOSCO Noto Sans", 9, XFontStyleEx.Regular,
            new XPdfFontOptions(PdfFontEncoding.Unicode));
    });

    internal static byte[] Write(IReadOnlyList<object?[]> rows)
    {
        var font = Font.Value;
        using var document = new PdfDocument();
        document.Info.Title = "HOSCO Executive Dashboard";
        XGraphics? graphics = null;
        PdfPage? page = null;
        double y = 0;
        const double margin = 40, lineHeight = 14;
        try
        {
            foreach (var row in rows)
            {
                if (page is null) AddPage();
                var lastValue = Array.FindLastIndex(row, x => x is not null);
                var text = string.Join(" | ", row.Take(lastValue + 1).Select(x => Convert.ToString(x, CultureInfo.InvariantCulture) ?? ""));
                foreach (var line in Wrap(text, graphics!, font, page!.Width.Point - 2 * margin))
                {
                    if (y + lineHeight > page.Height.Point - margin) AddPage();
                    graphics!.DrawString(line, font, XBrushes.Black,
                        new XRect(margin, y, page.Width.Point - 2 * margin, lineHeight), XStringFormats.TopLeft);
                    y += lineHeight;
                }
            }
        }
        finally { graphics?.Dispose(); }
        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();

        void AddPage()
        {
            graphics?.Dispose();
            page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            graphics = XGraphics.FromPdfPage(page);
            y = margin;
        }
    }

    private static IEnumerable<string> Wrap(string text, XGraphics graphics, XFont font, double width)
    {
        // Unicode text elements keep decomposed Vietnamese marks and surrogate pairs intact.
        foreach (var paragraph in text.Replace("\r", "").Split('\n'))
        {
            var line = new StringBuilder();
            var elements = StringInfo.GetTextElementEnumerator(paragraph);
            while (elements.MoveNext())
            {
                var element = elements.GetTextElement();
                if (line.Length > 0 && graphics.MeasureString(line.ToString() + element, font).Width > width)
                {
                    yield return line.ToString();
                    line.Clear();
                }
                line.Append(element);
            }
            yield return line.ToString();
        }
    }

    private sealed class DashboardFontResolver : IFontResolver
    {
        public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) =>
            familyName == "HOSCO Noto Sans" ? new FontResolverInfo("NotoSans-Regular") : null;

        public byte[]? GetFont(string faceName)
        {
            if (faceName != "NotoSans-Regular") return null;
            using var resource = typeof(DashboardPdfWriter).Assembly.GetManifestResourceStream(
                "Hosco.Infrastructure.Assets.Fonts.NotoSans-Regular.ttf")
                ?? throw new InvalidOperationException("Embedded dashboard PDF font is missing.");
            using var stream = new MemoryStream();
            resource.CopyTo(stream);
            return stream.ToArray();
        }
    }
}
