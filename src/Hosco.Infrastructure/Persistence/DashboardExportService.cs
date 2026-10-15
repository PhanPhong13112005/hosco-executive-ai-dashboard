using System.Globalization;
using System.IO.Compression;
using System.Text;
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
        var top = await data.GetProductRankingAsync(scope, filter with { Page = 1, PageSize = 10 }, false, cancellationToken);
        var bottom = await data.GetProductRankingAsync(scope, filter with { Page = 1, PageSize = 10 }, true, cancellationToken);
        var dangerous = await data.GetDangerousInventoryAsync(scope, filter with { PageSize = ReportingFilter.MaxPageSize }, cancellationToken);
        var branches = await data.GetBranchesAsync(scope, cancellationToken);
        var branch = filter.BranchId.HasValue
            ? branches.FirstOrDefault(x => x.Id == filter.BranchId)?.Name ?? filter.BranchId.Value.ToString()
            : "All authorized branches";
        var rows = BuildRows(filter, branch, summary, top.Items, bottom.Items, dangerous);
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
            new object?[] { "AOV", (object?)summary.Aov ?? "N/A", summary.Currency },
            new object?[] { "Gross profit", summary.GrossProfit, summary.Currency },
            new object?[] { "Gross margin", (object?)summary.GrossMarginPercent ?? "N/A", "%" },
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
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>
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
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>
                """);
            WriteEntry(archive, "xl/styles.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="1"><font><sz val="11"/><name val="Arial"/></font></fonts><fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills><borders count="1"><border/></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment vertical="top" wrapText="1"/></xf></cellXfs><cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles></styleSheet>
                """);
            // Keep dates, labels and SKU identifiers readable without changing data.
            var widths = Enumerable.Range(0, rows.Max(x => x.Length)).Select(column =>
                Math.Clamp(rows.Max(row => column < row.Length ?
                    (Convert.ToString(row[column], CultureInfo.InvariantCulture) ?? "").Length : 0) + 2, 12, 50)).ToArray();
            var sheet = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><cols>");
            for (var column = 0; column < widths.Length; column++)
                sheet.Append($"<col min=\"{column + 1}\" max=\"{column + 1}\" width=\"{widths[column]}\" customWidth=\"1\"/>");
            sheet.Append("</cols><sheetData>");
            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var lineCount = rows[rowIndex].Select((value, column) => Math.Max(1,
                    (int)Math.Ceiling((Convert.ToString(value, CultureInfo.InvariantCulture) ?? "").Length / (double)(widths[column] - 2)))).DefaultIfEmpty(1).Max();
                sheet.Append("<row r=\"").Append(rowIndex + 1).Append("\" ht=\"")
                    .Append(lineCount * 15).Append("\" customHeight=\"1\">");
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
        return DashboardPdfWriter.Write(rows);
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
}
