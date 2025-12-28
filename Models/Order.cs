using System.ComponentModel.DataAnnotations;

namespace POS_in_NET.Models;

public class Order
{
    public int Id { get; set; }
    
    [Required]
    public string OrderId { get; set; } = string.Empty; // External order ID from online system
    
    // OrderWeb.net fields
    public string? OrderNumber { get; set; } // Human readable order number (KIT-7479)
    public string? CloudOrderId { get; set; } // UUID from OrderWeb.net
    
    [Required]
    public string CustomerName { get; set; } = string.Empty;
    
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerAddress { get; set; }
    
    // Financial fields
    public decimal TotalAmount { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TaxAmount { get; set; }
    
    // Order details
    public string? OrderType { get; set; } // pickup/delivery
    public string? PaymentMethod { get; set; }
    public DateTime? ScheduledTime { get; set; }
    public string? SpecialInstructions { get; set; }
    
    public OrderStatus Status { get; set; } = OrderStatus.New;
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;
    
    // Order timing for automatic status transitions
    public DateTime? KitchenTime { get; set; }        // When order goes to kitchen (auto after 0 min)
    public DateTime? PreparingTime { get; set; }      // When order starts preparing (auto after 2 min)
    public DateTime? ReadyTime { get; set; }          // When order is ready (auto after 10 min)
    public DateTime? DeliveringTime { get; set; }     // When delivery person takes order (manual)
    public DateTime? CompletedTime { get; set; }      // When order is completed (manual)
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Order items
    public List<OrderItem> Items { get; set; } = new();
    
    // UI Display Properties
    public bool HasSpecialInstructions => !string.IsNullOrWhiteSpace(SpecialInstructions);
    public bool HasDeliveryFee => DeliveryFee > 0;
    public bool CanComplete => Status != OrderStatus.Completed && Status != OrderStatus.Cancelled;
    
    // Additional data from online system (JSON)
    public string? OrderData { get; set; }
    
    // Payment information
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? TransactionId { get; set; }
    
    // Print tracking (NEW for acknowledgment system)
    public string PrintStatus { get; set; } = "pending"; // pending, sent_to_pos, printing, printed, failed
    public DateTime? PrintedAt { get; set; }
    public string? PrintError { get; set; }
    public string? PrintDeviceId { get; set; }
}

public class OrderItem
{
    public int Id { get; set; }
    public string OrderId { get; set; } = string.Empty; // Changed to string to match UUID
    
    // OrderWeb.net fields
    public int? CloudItemId { get; set; }
    public string? MenuItemId { get; set; }
    
    [Required]
    public string ItemName { get; set; } = string.Empty;
    
    public int Quantity { get; set; }
    public decimal? ItemPrice { get; set; } // Renamed from UnitPrice
    public decimal TotalPrice => (ItemPrice ?? 0) * Quantity + Addons.Sum(a => (a.AddonPrice ?? 0) * Quantity);
    
    // UI Display Properties
    public string QuantityDisplay => $"{Quantity}x";
    
    public string? SpecialInstructions { get; set; }
    
    // Addons for this item
    public List<OrderItemAddon> Addons { get; set; } = new();
    
    // Navigation property
    public Order? Order { get; set; }
}

public class OrderItemAddon
{
    public int Id { get; set; }
    public int OrderItemId { get; set; }
    
    public string? AddonId { get; set; }
    public string AddonName { get; set; } = string.Empty;
    public decimal? AddonPrice { get; set; }
    public int Quantity { get; set; } = 1;
    
    // Navigation property
    public OrderItem? OrderItem { get; set; }
}

public enum OrderStatus
{
    New,         // Just received from online system
    Kitchen,     // Sent to kitchen (auto after order received)
    Preparing,   // Being prepared (auto after 2 minutes)
    Ready,       // Ready for delivery (auto after 10 minutes)
    Delivering,  // Out for delivery (manual by delivery person)
    Completed,   // Delivered successfully (manual)
    Cancelled    // Order cancelled
}

public enum SyncStatus
{
    Synced,      // Successfully synced with online system
    Pending,     // Waiting to be synced
    Failed,      // Sync failed, will retry
    Conflict     // Conflict detected, needs manual resolution
}

public enum PaymentStatus
{
    Pending,     // Payment not yet processed
    Paid,        // Payment completed
    Failed,      // Payment failed
    Refunded     // Payment refunded
}