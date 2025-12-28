using System;
using System.ComponentModel.DataAnnotations;

namespace POS_in_NET.Models
{
    /// <summary>
    /// Enhanced table session status tracking
    /// </summary>
    public enum TableSessionStatus
    {
        Occupied,       // Customers just seated, haven't ordered yet
        Ordering,       // Taking order, deciding what to eat
        FoodServed,     // Food delivered, customers eating
        Payment,        // Requesting bill, processing payment
        Cleaning,       // Table being cleaned after customers leave
        Closed          // Session completed and closed
    }

    /// <summary>
    /// Represents a dining session at a restaurant table
    /// </summary>
    public class TableSession
    {
        public int Id { get; set; }
        public int TableId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string SessionNumber { get; set; } = string.Empty; // S001, S002, etc.
        
        [Range(1, 20)]
        public int PartySize { get; set; } = 1;
        
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }
        
        public TableSessionStatus Status { get; set; } = TableSessionStatus.Occupied;
        
        public string? CustomerNotes { get; set; }
        
        [StringLength(100)]
        public string? SpecialOccasion { get; set; } // Birthday, Anniversary, etc.
        
        public int EstimatedDuration { get; set; } = 60; // Minutes
        public int? ActualDuration { get; set; } // Calculated when closed
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public RestaurantTable? Table { get; set; }
        
        // Calculated properties
        public int MinutesOccupied => EndTime.HasValue 
            ? (int)(EndTime.Value - StartTime).TotalMinutes 
            : (int)(DateTime.Now - StartTime).TotalMinutes;
            
        public string StatusDisplay => Status switch
        {
            TableSessionStatus.Occupied => "🟡 Just Seated",
            TableSessionStatus.Ordering => "🔵 Taking Order", 
            TableSessionStatus.FoodServed => "🟠 Dining",
            TableSessionStatus.Payment => "🟣 Ready to Pay",
            TableSessionStatus.Cleaning => "🔴 Cleaning",
            TableSessionStatus.Closed => "✅ Closed",
            _ => "❓ Unknown"
        };
        
        public string StatusColor => Status switch
        {
            TableSessionStatus.Occupied => "#FFF3CD",      // Light yellow
            TableSessionStatus.Ordering => "#CCE5FF",      // Light blue
            TableSessionStatus.FoodServed => "#FFE0CC",    // Light orange  
            TableSessionStatus.Payment => "#E6CCFF",       // Light purple
            TableSessionStatus.Cleaning => "#FFCCCC",      // Light red
            TableSessionStatus.Closed => "#D4EDDA",        // Light green
            _ => "#F8F9FA"                                 // Light gray
        };
        
        public string TimeDisplay
        {
            get
            {
                var minutes = MinutesOccupied;
                if (minutes < 60)
                    return $"{minutes}m";
                else
                    return $"{minutes / 60}h {minutes % 60}m";
            }
        }
        
        public bool IsOvertime => MinutesOccupied > EstimatedDuration;
        
        public string OvertimeWarning => IsOvertime 
            ? $"⚠️ {MinutesOccupied - EstimatedDuration} min over" 
            : "";
    }
}