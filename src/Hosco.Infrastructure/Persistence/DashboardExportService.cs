using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using Hosco.Application.Abstractions;
using Hosco.Application.Models;

namespace Hosco.Infrastructure.Persistence;

public sealed class DashboardExportService(IReportingDataStore data, IAlertRepository alerts) : IDashboardExportService
{
    public async Task<ExportDocument> ExportAsync(string format, ReportingScope scope, ReportingFilter filter,
        CancellationToken cancellationToken)
    {
        filter.Validate();
        var alertSummary = await alerts.GetSummaryAsync(scope, cancellationToken);
        var summary = await data.GetDashboardSummaryAsync(scope, filter, alertSummary, cancellationToken);
        var top = await data.GetProductRankingAsync(scope, filter with { PageSize = 10 }, false, cancellationToken);
        var bottom = await data.GetProductRankingAsync(scope, filter with { PageSize = 10 }, true, cancellationToken);
        var dangerous = await data.GetDangerousInventoryAsync(scope, filter with { PageSize = ReportingFilter.MaxPageSize }, cancellationToken);
        var branches = await data.GetBranchesAsync(scope, cancellationToken);
        var branch = filter.BranchId.HasValue
            ? branches.FirstOrDefault(x => x.Id == filter.BranchId)?.Name ?? filter.BranchId.Value.ToString()
            : "All authorized branches";
        var rows = BuildRows(filter, branch, summary, top, bottom, dangerous);
        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        return format.Trim().ToLowerInvariant() switch
        {
            "xlsx" => new ExportDocument(CreateXlsx(rows),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"hosco-dashboard-{stamp}.xlsx"),
            "pdf" => new ExportDocument(CreatePdf(rows), "application/pdf", $"hosco-dashboard-{stamp}.pdf"),
            _ => throw new ValidationException("format must be 'xlsx' or 'pdf'.")
        };
    }

    private static List<object?[]> BuildRows(ReportingFilter filter, string branch, DashboardSummary summary,
        IReadOnlyList<ProductRankRow> top, IReadOnlyList<ProductRankRow> bottom,
        IReadOnlyList<DangerousInventoryRow> dangerous)
    {
        var rows = new List<object?[]>
        {
            new object?[] { "HOSCO Executive Dashboard", null, null, null, null, null },
            new object?[] { "From", filter.From?.ToString("O"), "To", filter.To?.ToString("O"), "Branch", branch },
            Array.Empty<object?>(),
            new object?[] { "KPI", "Value", "Unit" },
            new object?[] { "Revenue", summary.Revenue, summary.Currency },
            new object?[] { "GMV", summary.Gmv, summary.Currency },
            new object?[] { "Total orders", summary.TotalOrders, "orders" },
            new object?[] { "AOV", summary.Aov, summary.Currency },
            new object?[] { "Gross profit", summary.GrossProfit, summary.Currency },
            new object?[] { "Gross margin", summary.GrossMarginPercent, "%" },
            new object?[] { "Cancellation / return rate", summary.CancellationReturnRate, "%" },
            new object?[] { "Dangerous stock", summary.DangerousStockCount, "SKU" },
            new object?[] { "Open alerts", summary.OpenAlerts, "alerts" },
            new object?[] { "Urgent alerts", summary.UrgentAlerts, "alerts" },
            Array.Empty<object?>(),
            new object?[] { "Top 10 SKU", "SKU", "Product", "Valid quantity", "Amount", "Currency" }
        };
        rows.AddRange(top.Select((x, i) => new object?[] { i + 1, x.Sku, x.Name, x.Quantity, x.Amount, x.Currency }));
        rows.Add(Array.Empty<object?>());
        rows.Add(new object?[] { "Bottom 10 SKU", "SKU", "Product", "Valid quantity", "Amount", "Currency" });
        rows.AddRange(bottom.Select((x, i) => new object?[] { i + 1, x.Sku, x.Name, x.Quantity, x.Amount, x.Currency }));
        rows.Add(Array.Empty<object?>());
        rows.Add(new object?[] { "Dangerous inventory", "Branch", "SKU", "Product", "Available", "Safety stock" });
        rows.AddRange(dangerous.Select((x, i) => new object?[]
            { i + 1, x.BranchId, x.Sku, x.ProductName, x.AvailableQuantity, x.SafetyStock }));
        return rows;
    }

    private static byte[] CreateXlsx(IReadOnlyList<object?[]> rows)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            WriteEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>
                """);
            WriteEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
                """);
            WriteEntry(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Dashboard" sheetId="1" r:id="rId1"/></sheets></workbook>
                """);
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>
                """);
            var sheet = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                sheet.Append("<row r=\"").Append(rowIndex + 1).Append("\">");
                for (var column = 0; column < rows[rowIndex].Length; column++)
                {
                    var value = rows[rowIndex][column];
                    if (value is null) continue;
                    var reference = $"{ColumnName(column + 1)}{rowIndex + 1}";
                    if (value is byte or short or int or long or float or double or decimal)
                        sheet.Append("<c r=\"").Append(reference).Append("\"><v>")
                            .Append(Convert.ToString(value, CultureInfo.InvariantCulture)).Append("</v></c>");
                    else
                        sheet.Append("<c r=\"").Append(reference).Append("\" t=\"inlineStr\"><is><t>")
                            .Append(XmlEscape(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty))
                            .Append("</t></is></c>");
                }
                sheet.Append("</row>");
            }
            sheet.Append("</sheetData></worksheet>");
            WriteEntry(archive, "xl/worksheets/sheet1.xml", sheet.ToString());
        }
        return output.ToArray();
    }

    private static byte[] CreatePdf(IReadOnlyList<object?[]> rows)
    {
        var lines = rows.Select(row => string.Join(" | ", row.Where(x => x is not null).Select(x => Ascii(Convert.ToString(x, CultureInfo.InvariantCulture) ?? string.Empty))))
            .Where(x => x.Length > 0).Take(48).ToList();
        var content = new StringBuilder("BT /F1 9 Tf 40 800 Td 12 TL ");
        foreach (var line in lines)
            content.Append('(').Append(PdfEscape(line.Length > 105 ? line[..105] : line)).Append(") Tj T* ");
        content.Append("ET");
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content.ToString())} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };
        using var stream = new MemoryStream();
        WriteAscii(stream, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(stream.Position);
            WriteAscii(stream, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }
        var xref = stream.Position;
        WriteAscii(stream, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) WriteAscii(stream, $"{offset:0000000000} 00000 n \n");
        WriteAscii(stream, $"trailer << /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return stream.ToArray();
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(path).Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string ColumnName(int column)
    {
        var result = string.Empty;
        while (column > 0) { column--; result = (char)('A' + column % 26) + result; column /= 26; }
        return result;
    }

    private static string XmlEscape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    private static string PdfEscape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static string Ascii(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        return new string(normalized.Where(x => CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark && x <= 127).ToArray());
    }
    private static void WriteAscii(Stream stream, string value) => stream.Write(Encoding.ASCII.GetBytes(value));
}
