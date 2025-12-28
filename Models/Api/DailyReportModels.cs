using System.Text.Json.Serialization;

namespace POS_in_NET.Models.Api;

/// <summary>
/// Comprehensive daily report to send to OrderWeb.net
/// </summary>
public class DailyReport
{
    [JsonPropertyName("reportDate")]
    public DateTime ReportDate { get; set; }

    [JsonPropertyName("restaurantId")]
    public string RestaurantId { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public DailySummary Summary { get; set; } = new();

    [JsonPropertyName("sales")]
    public SalesData Sales { get; set; } = new();

    [JsonPropertyName("orders")]
    public OrderData Orders { get; set; } = new();

    [JsonPropertyName("items")]
    public List<ItemSalesData> ItemSales { get; set; } = new();

    [JsonPropertyName("hourlyBreakdown")]
    public List<HourlySales> HourlyBreakdown { get; set; } = new();

    [JsonPropertyName("paymentMethods")]
    public List<PaymentMethodData> PaymentMethods { get; set; } = new();

    [JsonPropertyName("generatedAt")]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("reportType")]
    public string ReportType { get; set; } = "daily"; // "daily", "manual"
}

/// <summary>
/// Daily summary statistics
/// </summary>
public class DailySummary
{
    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("totalOrders")]
    public int TotalOrders { get; set; }

    [JsonPropertyName("averageOrderValue")]
    public decimal AverageOrderValue { get; set; }

    [JsonPropertyName("totalTax")]
    public decimal TotalTax { get; set; }

    [JsonPropertyName("totalTips")]
    public decimal TotalTips { get; set; }

    [JsonPropertyName("totalDiscounts")]
    public decimal TotalDiscounts { get; set; }

    [JsonPropertyName("busyHour")]
    public string BusyHour { get; set; } = string.Empty;

    [JsonPropertyName("peakOrderTime")]
    public string PeakOrderTime { get; set; } = string.Empty;
}

/// <summary>
/// Sales data breakdown
/// </summary>
public class SalesData
{
    [JsonPropertyName("webOrders")]
    public decimal WebOrderSales { get; set; }

    [JsonPropertyName("posOrders")]
    public decimal PosOrderSales { get; set; }

    [JsonPropertyName("deliverySales")]
    public decimal DeliverySales { get; set; }

    [JsonPropertyName("pickupSales")]
    public decimal PickupSales { get; set; }

    [JsonPropertyName("dineInSales")]
    public decimal DineInSales { get; set; }

    [JsonPropertyName("refunds")]
    public decimal Refunds { get; set; }
}

/// <summary>
/// Order statistics
/// </summary>
public class OrderData
{
    [JsonPropertyName("webOrderCount")]
    public int WebOrderCount { get; set; }

    [JsonPropertyName("posOrderCount")]
    public int PosOrderCount { get; set; }

    [JsonPropertyName("deliveryCount")]
    public int DeliveryCount { get; set; }

    [JsonPropertyName("pickupCount")]
    public int PickupCount { get; set; }

    [JsonPropertyName("dineInCount")]
    public int DineInCount { get; set; }

    [JsonPropertyName("cancelledCount")]
    public int CancelledCount { get; set; }

    [JsonPropertyName("refundedCount")]
    public int RefundedCount { get; set; }

    [JsonPropertyName("averagePreparationTime")]
    public double AveragePreparationTimeMinutes { get; set; }
}

/// <summary>
/// Individual item sales data
/// </summary>
public class ItemSalesData
{
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = string.Empty;

    [JsonPropertyName("itemName")]
    public string ItemName { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("quantitySold")]
    public int QuantitySold { get; set; }

    [JsonPropertyName("revenue")]
    public decimal Revenue { get; set; }

    [JsonPropertyName("averagePrice")]
    public decimal AveragePrice { get; set; }

    [JsonPropertyName("popularModifiers")]
    public List<string> PopularModifiers { get; set; } = new();
}

/// <summary>
/// Hourly sales breakdown
/// </summary>
public class HourlySales
{
    [JsonPropertyName("hour")]
    public int Hour { get; set; } // 0-23

    [JsonPropertyName("orderCount")]
    public int OrderCount { get; set; }

    [JsonPropertyName("revenue")]
    public decimal Revenue { get; set; }

    [JsonPropertyName("averageOrderValue")]
    public decimal AverageOrderValue { get; set; }
}

/// <summary>
/// Payment method breakdown
/// </summary>
public class PaymentMethodData
{
    [JsonPropertyName("paymentMethod")]
    public string PaymentMethod { get; set; } = string.Empty;

    [JsonPropertyName("orderCount")]
    public int OrderCount { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("percentage")]
    public decimal Percentage { get; set; }
}

/// <summary>
/// Report upload response from OrderWeb.net
/// </summary>
public class ReportUploadResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("reportId")]
    public string ReportId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}