using System.Text.Json.Serialization;

namespace POS_in_NET.Models.Api;

/// <summary>
/// Represents an incoming order from OrderWeb.net
/// </summary>
public class WebOrderRequest
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("customerPhone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [JsonPropertyName("customerEmail")]
    public string CustomerEmail { get; set; } = string.Empty;

    [JsonPropertyName("orderType")]
    public string OrderType { get; set; } = string.Empty; // "delivery", "pickup", "dine-in"

    [JsonPropertyName("orderTime")]
    public DateTime OrderTime { get; set; }

    [JsonPropertyName("requestedTime")]
    public DateTime? RequestedTime { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("tax")]
    public decimal Tax { get; set; }

    [JsonPropertyName("deliveryFee")]
    public decimal DeliveryFee { get; set; }

    [JsonPropertyName("tip")]
    public decimal Tip { get; set; }

    [JsonPropertyName("items")]
    public List<WebOrderItem> Items { get; set; } = new();

    [JsonPropertyName("specialInstructions")]
    public string SpecialInstructions { get; set; } = string.Empty;

    [JsonPropertyName("deliveryAddress")]
    public DeliveryAddress? DeliveryAddress { get; set; }

    [JsonPropertyName("paymentMethod")]
    public string PaymentMethod { get; set; } = string.Empty;

    [JsonPropertyName("paymentStatus")]
    public string PaymentStatus { get; set; } = string.Empty;
}

/// <summary>
/// Represents an item in a web order
/// </summary>
public class WebOrderItem
{
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("totalPrice")]
    public decimal TotalPrice { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("modifiers")]
    public List<OrderModifier> Modifiers { get; set; } = new();

    [JsonPropertyName("specialInstructions")]
    public string SpecialInstructions { get; set; } = string.Empty;
}

/// <summary>
/// Represents a modifier for an order item
/// </summary>
public class OrderModifier
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// Response when receiving an order
/// </summary>
public class WebOrderResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("estimatedTime")]
    public int? EstimatedTimeMinutes { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Delivery address for web orders
/// </summary>
public class DeliveryAddress
{
    [JsonPropertyName("street")]
    public string Street { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("postcode")]
    public string Postcode { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}