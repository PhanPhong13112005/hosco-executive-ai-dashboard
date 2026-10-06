using System.Globalization;
using System.Text.Json;
using Hosco.Application.Abstractions;
using Hosco.Application.Models;

namespace Hosco.Application.Services;

public sealed class ChatResponseComposer : IResponseComposer
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public string Compose(ChatIntentResult intent, ReportingApiResult result)
    {
        if (result.Data.ValueKind == JsonValueKind.Object &&
            result.Data.TryGetProperty("totalOrders", out var orders) && orders.GetInt32() == 0 &&
            intent.Intent is not ChatIntent.CurrentAlerts)
            return "Không có dữ liệu phù hợp trong khoảng thời gian này.";
        return intent.Intent switch
    {
        ChatIntent.KpiOverview => Overview(result.Data, intent.DateRange?.Label),
        ChatIntent.Revenue => Scalar(result.Data, "revenue", "Doanh thu", intent.DateRange?.Label, "currency"),
        ChatIntent.Gmv => Scalar(result.Data, "gmv", "GMV", intent.DateRange?.Label, "currency"),
        ChatIntent.TotalOrders => Scalar(result.Data, "totalOrders", "Tổng số đơn", intent.DateRange?.Label),
        ChatIntent.Aov => Scalar(result.Data, "aov", "AOV", intent.DateRange?.Label, "currency"),
        ChatIntent.GrossProfit => Scalar(result.Data, "grossProfit", "Lợi nhuận gộp", intent.DateRange?.Label, "currency"),
        ChatIntent.GrossMargin => Scalar(result.Data, "grossMarginPercent", "Biên lợi nhuận gộp", intent.DateRange?.Label, suffix: "%"),
        ChatIntent.CancellationReturnRate => Scalar(result.Data, "cancellationReturnRate", "Tỷ lệ hủy/hoàn", intent.DateRange?.Label, suffix: "%"),
        ChatIntent.RevenueTrend => Trend(result.Data, intent.DateRange?.Label),
        ChatIntent.TopProducts => Products(result.Data, "Top sản phẩm"),
        ChatIntent.BottomProducts => Products(result.Data, "Bottom sản phẩm"),
        ChatIntent.DangerousInventory => Inventory(result.Data),
        ChatIntent.CurrentAlerts => Alerts(result.Data, intent.Severity),
        _ => "Hiện tại tôi chưa hỗ trợ loại câu hỏi này."
        };
    }

    private static string Overview(JsonElement data, string? period) =>
        $"Tổng quan {period}: doanh thu {Money(data.GetProperty("revenue").GetDecimal(), Currency(data))}, " +
        $"GMV {Money(data.GetProperty("gmv").GetDecimal(), Currency(data))}, " +
        $"{data.GetProperty("totalOrders").GetInt32():N0} đơn, lợi nhuận gộp " +
        $"{Money(data.GetProperty("grossProfit").GetDecimal(), Currency(data))}.";

    private static string Scalar(JsonElement data, string property, string label, string? period,
        string? currencyProperty = null, string? suffix = null)
    {
        var value = data.GetProperty(property);
        if (value.ValueKind == JsonValueKind.Null) return $"Không có dữ liệu phù hợp cho {label.ToLowerInvariant()} {period}.";
        var number = value.GetDecimal();
        var formatted = currencyProperty is null ? number.ToString("N2", Vietnamese) : Money(number, Currency(data));
        return $"{label} {period} là {formatted}{suffix}.";
    }

    private static string Trend(JsonElement data, string? period)
    {
        var points = data.EnumerateArray().ToList();
        if (points.Count == 0) return $"Không có dữ liệu doanh thu phù hợp {period}.";
        var latest = points[^1];
        return $"Xu hướng doanh thu {period} có {points.Count} điểm dữ liệu; ngày gần nhất " +
               $"{latest.GetProperty("date").GetString()} đạt {Money(latest.GetProperty("amount").GetDecimal(), latest.GetProperty("currency").GetString() ?? "VND")}.";
    }

    private static string Products(JsonElement data, string label)
    {
        var rows = data.EnumerateArray().ToList();
        if (rows.Count == 0) return "Không có dữ liệu sản phẩm phù hợp trong khoảng thời gian này.";
        return $"{label}: " + string.Join("; ", rows.Select((row, index) =>
            $"{index + 1}. {row.GetProperty("name").GetString()} ({row.GetProperty("sku").GetString()}) – {row.GetProperty("quantity").GetInt32():N0} sản phẩm")) + ".";
    }

    private static string Inventory(JsonElement data)
    {
        var rows = data.EnumerateArray().ToList();
        if (rows.Count == 0) return "Không có sản phẩm nào đang ở mức tồn kho nguy hiểm trong phạm vi của bạn.";
        return $"Có {rows.Count} dòng tồn kho nguy hiểm: " + string.Join("; ", rows.Select(row =>
            $"{row.GetProperty("productName").GetString()} ({row.GetProperty("sku").GetString()}) còn khả dụng {row.GetProperty("availableQuantity").GetInt32():N0}")) + ".";
    }

    private static string Alerts(JsonElement data, string? severity)
    {
        var items = data.GetProperty("items").EnumerateArray().ToList();
        if (items.Count == 0) return severity is null
            ? "Hiện không có cảnh báo đang mở trong phạm vi của bạn."
            : $"Hiện không có cảnh báo {severity} đang mở trong phạm vi của bạn.";
        var total = data.TryGetProperty("totalCount", out var count) ? count.GetInt32() : items.Count;
        return $"Có {total} cảnh báo đang mở" + (severity is null ? "" : $" mức {severity}") + ": " +
               string.Join("; ", items.Select(x => $"{x.GetProperty("ruleCode").GetString()} – {x.GetProperty("title").GetString()}")) + ".";
    }

    private static string Currency(JsonElement data) => data.TryGetProperty("currency", out var currency)
        ? currency.GetString() ?? "VND" : "VND";

    private static string Money(decimal value, string currency) =>
        $"{value.ToString("N0", Vietnamese)} {currency}";
}
