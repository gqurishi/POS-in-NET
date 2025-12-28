using System.Text.Json.Serialization;
using POS_in_NET.Converters;

namespace POS_in_NET.Models.Api;

/// <summary>
/// Response from orderweb.net REST API polling endpoint
/// Endpoint: GET /api/pos/pull-orders
/// </summary>
public class PullOrdersResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("orders")]
    public List<PullOrderDto> Orders { get; set; } = new();
    
    [JsonPropertyName("count")]
    public int Count { get; set; }
    
    [JsonPropertyName("server_time")]
    public string? ServerTime { get; set; }
    
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; set; }
}

/// <summary>
/// Individual order data from pull-orders endpoint
/// </summary>
public class PullOrderDto
{
    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }
    
    [JsonPropertyName("tenant")]
    public string? Tenant { get; set; }
    
    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }
    
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
    
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
    
    [JsonPropertyName("order_status")]
    public string? OrderStatus { get; set; }
    
    [JsonPropertyName("print_status")]
    public string? PrintStatus { get; set; }
    
    [JsonPropertyName("order_type")]
    public string? OrderType { get; set; }
    
    [JsonPropertyName("items")]
    public List<OrderItemDto> Items { get; set; } = new();
    
    [JsonPropertyName("customer")]
    public CustomerDto? Customer { get; set; }
    
    [JsonPropertyName("payment")]
    public PaymentDto? Payment { get; set; }
    
    [JsonPropertyName("special_instructions")]
    public string? SpecialInstructions { get; set; }
    
    [JsonPropertyName("scheduled_for")]
    public string? ScheduledFor { get; set; }
}

public class OrderItemDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
    
    [JsonPropertyName("price")]
    [JsonConverter(typeof(StringToDoubleConverter))]
    public double Price { get; set; }
    
    [JsonPropertyName("modifiers")]
    public List<ModifierDto> Modifiers { get; set; } = new();
    
    [JsonPropertyName("special_instructions")]
    public string? SpecialInstructions { get; set; }
}

public class ModifierDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("price")]
    [JsonConverter(typeof(StringToDoubleConverter))]
    public double Price { get; set; }
}

public class CustomerDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    [JsonPropertyName("address")]
    public string? Address { get; set; }
}

public class PaymentDto
{
    [JsonPropertyName("method")]
    public string? Method { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    [JsonPropertyName("total")]
    [JsonConverter(typeof(StringToDoubleConverter))]
    public double Total { get; set; }
    
    [JsonPropertyName("subtotal")]
    [JsonConverter(typeof(StringToDoubleConverter))]
    public double Subtotal { get; set; }
    
    [JsonPropertyName("tax")]
    [JsonConverter(typeof(StringToDoubleConverter))]
    public double Tax { get; set; }
    
    [JsonPropertyName("tip")]
    [JsonConverter(typeof(StringToDoubleConverter))]
    public double Tip { get; set; }
}

/// <summary>
/// Model for pending acknowledgments stored locally
/// </summary>
public class PendingAck
{
    public int Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "printed" or "failed"
    public string? Reason { get; set; }
    public DateTime? PrintedAt { get; set; }
    public string? DeviceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastRetryAt { get; set; }
}
