using POS_in_NET.Models;

namespace POS_in_NET.Models.Api;

// API Request/Response models for communicating with online ordering system

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; }
}

public class OrdersResponse
{
    public List<ApiOrder> Orders { get; set; } = new();
    public int TotalCount { get; set; }
    public DateTime LastSyncTime { get; set; }
}

public class ApiOrder
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerAddress { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public List<ApiOrderItem> Items { get; set; } = new();
    public string? SpecialInstructions { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }
    public string? TransactionId { get; set; }
    
    // Convert to local Order model
    public Order ToOrder()
    {
        return new Order
        {
            OrderId = this.OrderId,
            CustomerName = this.CustomerName,
            CustomerPhone = this.CustomerPhone,
            CustomerEmail = this.CustomerEmail,
            CustomerAddress = this.CustomerAddress,
            TotalAmount = this.TotalAmount,
            Status = ParseOrderStatus(this.Status),
            PaymentStatus = ParsePaymentStatus(this.PaymentStatus),
            PaymentMethod = this.PaymentMethod,
            TransactionId = this.TransactionId,
            SpecialInstructions = this.SpecialInstructions,
            CreatedAt = this.OrderDate,
            UpdatedAt = DateTime.Now,
            KitchenTime = DateTime.Now, // New orders go to kitchen immediately
            Items = this.Items.Select(item => item.ToOrderItem()).ToList()
        };
    }
    
    private static OrderStatus ParseOrderStatus(string? status)
    {
        return status?.ToLower() switch
        {
            "new" or "pending" => OrderStatus.New,
            "kitchen" => OrderStatus.Kitchen,
            "preparing" => OrderStatus.Preparing,
            "ready" => OrderStatus.Ready,
            "delivering" => OrderStatus.Delivering,
            "completed" or "delivered" => OrderStatus.Completed,
            "cancelled" => OrderStatus.Cancelled,
            _ => OrderStatus.New
        };
    }
    
    private Models.PaymentStatus ParsePaymentStatus(string? status)
    {
        return status?.ToLower() switch
        {
            "paid" or "completed" => Models.PaymentStatus.Paid,
            "pending" => Models.PaymentStatus.Pending,
            "failed" => Models.PaymentStatus.Failed,
            "refunded" => Models.PaymentStatus.Refunded,
            _ => Models.PaymentStatus.Pending
        };
    }
}

public class ApiOrderItem
{
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? SpecialInstructions { get; set; }
    public string? Category { get; set; }
    public string? ItemCode { get; set; }
    
    // Convert to local OrderItem model
    public POS_in_NET.Models.OrderItem ToOrderItem()
    {
        return new POS_in_NET.Models.OrderItem
        {
            ItemName = this.ItemName,
            Quantity = this.Quantity,
            ItemPrice = this.UnitPrice,
            SpecialInstructions = this.SpecialInstructions,
            MenuItemId = this.ItemCode,
            Addons = new List<POS_in_NET.Models.OrderItemAddon>()
        };
    }
}

public class OrderStatusUpdate
{
    public string OrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime UpdateTime { get; set; }
    public string? Notes { get; set; }
    public string? UpdatedBy { get; set; }
}

public class ApiConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public int SyncIntervalMinutes { get; set; } = 1;
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableAutoSync { get; set; } = true;
    public string? RestaurantId { get; set; }
    public string? LocationId { get; set; }
}

// Response model for OrderWeb.net pull orders API
public class OrderWebApiResponse
{
    public bool Success { get; set; }
    public TenantInfo Tenant { get; set; } = new();
    public List<CloudOrderResponse> Orders { get; set; } = new(); // Changed from PendingOrders to Orders
    public List<CloudOrderResponse> PendingOrders { get; set; } = new(); // Keep for backward compatibility
    public int Count { get; set; }
    public DateTime Timestamp { get; set; }
}

public class TenantInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
}

public class CloudOrderResponse
{
    public string Id { get; set; } = "";
    public string OrderNumber { get; set; } = "";
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Address { get; set; } // Changed from DeliveryAddress
    public string Total { get; set; } = "0"; // String in API response
    public string? Subtotal { get; set; }
    public string? DeliveryFee { get; set; }
    public string? Tax { get; set; }
    public string? Status { get; set; }
    public string? OrderType { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; } // Payment status: paid, pending, etc.
    public string? VoucherCode { get; set; } // Gift card code if used
    public string? SpecialInstructions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ScheduledTime { get; set; }
    public List<CloudOrderItem> Items { get; set; } = new();
    
    // Legacy properties for backward compatibility
    public string OrderId => OrderNumber;
    public string? DeliveryAddress => Address;
    public decimal TotalAmount => decimal.TryParse(Total, out var amount) ? amount : 0;
    public DateTime OrderTime => CreatedAt;
}

public class CloudOrderItem
{
    public int Id { get; set; }
    public string? MenuItemId { get; set; }
    public string Name { get; set; } = "";
    public decimal? Price { get; set; } // Individual item price - will be added by OrderWeb.net
    public int Quantity { get; set; }
    public List<CloudOrderAddon> SelectedAddons { get; set; } = new();
    public string? SpecialInstructions { get; set; }
    
    // Calculate total price for this item including addons
    public decimal GetTotalPrice()
    {
        var basePrice = (Price ?? 0) * Quantity;
        var addonPrice = SelectedAddons.Sum(addon => (addon.Price ?? 0) * Quantity);
        return basePrice + addonPrice;
    }
}

public class CloudOrderAddon
{
    public string? Id { get; set; }
    public string Name { get; set; } = "";
    public decimal? Price { get; set; } // Addon price - will be added by OrderWeb.net
}

public class DailySalesRequest
{
    public DateTime SaleDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int TotalItems { get; set; }
    public List<CategorySales> CategoryBreakdown { get; set; } = new();
    public List<ItemSales> TopItems { get; set; } = new();
}

public class CategorySales
{
    public string Category { get; set; } = "";
    public decimal Revenue { get; set; }
    public int ItemsSold { get; set; }
}

public class ItemSales
{
    public string ItemName { get; set; } = "";
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}