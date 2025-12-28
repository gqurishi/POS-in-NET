using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace POS_in_NET.Models
{
    /// <summary>
    /// Represents an order for a table (dine-in order)
    /// </summary>
    public class TableOrder : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private int _tableNumber;
        private string _staffName = string.Empty;
        private int _staffId;
        private int _coverCount;
        private DateTime _startTime;
        private decimal _subtotal;
        private decimal _serviceCharge;
        private decimal _serviceChargePercent;
        private decimal _discount;
        private decimal _discountPercent;
        private string? _discountReason;
        private decimal _total;
        private decimal _vat;
        private decimal _tipAmount;
        private TableOrderStatus _status = TableOrderStatus.Active;
        private string? _customerName;
        private string? _customerPhone;
        private string? _loyaltyCardNumber;
        private int _loyaltyPointsEarned;
        private int _loyaltyPointsRedeemed;
        private string? _notes;
        private DateTime _createdAt;
        private DateTime _updatedAt;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public int TableNumber
        {
            get => _tableNumber;
            set { _tableNumber = value; OnPropertyChanged(); }
        }

        public string StaffName
        {
            get => _staffName;
            set { _staffName = value; OnPropertyChanged(); }
        }

        public int StaffId
        {
            get => _staffId;
            set { _staffId = value; OnPropertyChanged(); }
        }

        public int CoverCount
        {
            get => _coverCount;
            set { _coverCount = value; OnPropertyChanged(); }
        }

        public DateTime StartTime
        {
            get => _startTime;
            set { _startTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(ElapsedTime)); }
        }

        public TimeSpan ElapsedTime => DateTime.Now - StartTime;

        public string ElapsedTimeDisplay
        {
            get
            {
                var elapsed = ElapsedTime;
                return $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
            }
        }

        public decimal Subtotal
        {
            get => _subtotal;
            set { _subtotal = value; OnPropertyChanged(); CalculateTotal(); }
        }

        public decimal ServiceCharge
        {
            get => _serviceCharge;
            set { _serviceCharge = value; OnPropertyChanged(); }
        }

        public decimal ServiceChargePercent
        {
            get => _serviceChargePercent;
            set 
            { 
                _serviceChargePercent = value; 
                OnPropertyChanged(); 
                CalculateServiceCharge();
            }
        }

        public decimal Discount
        {
            get => _discount;
            set { _discount = value; OnPropertyChanged(); CalculateTotal(); }
        }

        public decimal DiscountPercent
        {
            get => _discountPercent;
            set { _discountPercent = value; OnPropertyChanged(); }
        }

        public string? DiscountReason
        {
            get => _discountReason;
            set { _discountReason = value; OnPropertyChanged(); }
        }

        public decimal Total
        {
            get => _total;
            set { _total = value; OnPropertyChanged(); OnPropertyChanged(nameof(PerPersonAmount)); }
        }

        public decimal VAT
        {
            get => _vat;
            set { _vat = value; OnPropertyChanged(); }
        }

        public decimal TipAmount
        {
            get => _tipAmount;
            set { _tipAmount = value; OnPropertyChanged(); }
        }

        public decimal PerPersonAmount => CoverCount > 0 ? Total / CoverCount : Total;

        public TableOrderStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public string? CustomerName
        {
            get => _customerName;
            set { _customerName = value; OnPropertyChanged(); }
        }

        public string? CustomerPhone
        {
            get => _customerPhone;
            set { _customerPhone = value; OnPropertyChanged(); }
        }

        public string? LoyaltyCardNumber
        {
            get => _loyaltyCardNumber;
            set { _loyaltyCardNumber = value; OnPropertyChanged(); }
        }

        public int LoyaltyPointsEarned
        {
            get => _loyaltyPointsEarned;
            set { _loyaltyPointsEarned = value; OnPropertyChanged(); }
        }

        public int LoyaltyPointsRedeemed
        {
            get => _loyaltyPointsRedeemed;
            set { _loyaltyPointsRedeemed = value; OnPropertyChanged(); }
        }

        public string? Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(); }
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set { _updatedAt = value; OnPropertyChanged(); }
        }

        public ObservableCollection<TableOrderItem> Items { get; set; } = new();

        // Payment tracking
        public ObservableCollection<TableOrderPayment> Payments { get; set; } = new();
        
        public decimal TotalPaid => Payments.Sum(p => p.Amount);
        public decimal RemainingBalance => Total - TotalPaid;
        public bool IsFullyPaid => RemainingBalance <= 0;

        public void CalculateSubtotal()
        {
            Subtotal = Items.Sum(i => i.TotalPrice);
        }

        public void CalculateServiceCharge()
        {
            if (ServiceChargePercent > 0)
            {
                ServiceCharge = Math.Round(Subtotal * (ServiceChargePercent / 100), 2);
            }
            else
            {
                ServiceCharge = 0;
            }
            CalculateTotal();
        }

        public void CalculateTotal()
        {
            // Table orders are always dine-in = 20% VAT on subtotal
            VAT = Math.Round(Subtotal * 0.20m, 2);
            Total = Subtotal + VAT + ServiceCharge - Discount;
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(VAT));
            OnPropertyChanged(nameof(PerPersonAmount));
            OnPropertyChanged(nameof(RemainingBalance));
        }

        public void RecalculateAll()
        {
            CalculateSubtotal();
            CalculateServiceCharge();
            CalculateTotal();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents an item in a table order
    /// </summary>
    public class TableOrderItem : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _orderId = string.Empty;
        private string _menuItemId = string.Empty;
        private string _categoryId = string.Empty;
        private string _categoryColor = "#3B82F6";
        private string _name = string.Empty;
        private decimal _unitPrice;
        private int _quantity = 1;
        private string? _notes;
        private string? _modifiers;
        private ItemSendStatus _sendStatus = ItemSendStatus.NotSent;
        private string? _courseType;
        private DateTime _createdAt;
        private DateTime? _sentAt;
        private string? _voidReason;
        private bool _isVoided;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string OrderId
        {
            get => _orderId;
            set { _orderId = value; OnPropertyChanged(); }
        }

        public string MenuItemId
        {
            get => _menuItemId;
            set { _menuItemId = value; OnPropertyChanged(); }
        }

        public string CategoryId
        {
            get => _categoryId;
            set { _categoryId = value; OnPropertyChanged(); }
        }

        public string CategoryColor
        {
            get => _categoryColor;
            set { _categoryColor = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set { _unitPrice = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPrice)); }
        }

        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPrice)); }
        }

        public decimal TotalPrice => UnitPrice * Quantity + SelectedAddons.Sum(a => a.Price * Quantity);

        public string? Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNotes)); }
        }

        public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

        public string? Modifiers
        {
            get => _modifiers;
            set { _modifiers = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasModifiers)); }
        }

        public bool HasModifiers => !string.IsNullOrWhiteSpace(Modifiers);

        public ObservableCollection<SelectedAddon> SelectedAddons { get; set; } = new();

        public string ModifiersDisplay
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(Modifiers))
                    parts.Add(Modifiers);
                if (SelectedAddons.Any())
                    parts.AddRange(SelectedAddons.Select(a => a.Name));
                return string.Join(", ", parts);
            }
        }

        public ItemSendStatus SendStatus
        {
            get => _sendStatus;
            set { _sendStatus = value; OnPropertyChanged(); }
        }

        public string? CourseType
        {
            get => _courseType;
            set { _courseType = value; OnPropertyChanged(); }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(); }
        }

        public DateTime? SentAt
        {
            get => _sentAt;
            set { _sentAt = value; OnPropertyChanged(); }
        }

        public string? VoidReason
        {
            get => _voidReason;
            set { _voidReason = value; OnPropertyChanged(); }
        }

        public bool IsVoided
        {
            get => _isVoided;
            set { _isVoided = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a selected addon for an order item
    /// </summary>
    public class SelectedAddon
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    /// <summary>
    /// Represents a payment for a table order
    /// </summary>
    public class TableOrderPayment : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _orderId = string.Empty;
        private PaymentMethodType _method;
        private decimal _amount;
        private decimal _amountReceived;
        private decimal _change;
        private string? _reference;
        private string? _giftCardNumber;
        private DateTime _createdAt;
        private string _staffName = string.Empty;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string OrderId
        {
            get => _orderId;
            set { _orderId = value; OnPropertyChanged(); }
        }

        public PaymentMethodType Method
        {
            get => _method;
            set { _method = value; OnPropertyChanged(); }
        }

        public decimal Amount
        {
            get => _amount;
            set { _amount = value; OnPropertyChanged(); }
        }

        public decimal AmountReceived
        {
            get => _amountReceived;
            set { _amountReceived = value; OnPropertyChanged(); }
        }

        public decimal Change
        {
            get => _change;
            set { _change = value; OnPropertyChanged(); }
        }

        public string? Reference
        {
            get => _reference;
            set { _reference = value; OnPropertyChanged(); }
        }

        public string? GiftCardNumber
        {
            get => _giftCardNumber;
            set { _giftCardNumber = value; OnPropertyChanged(); }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(); }
        }

        public string StaffName
        {
            get => _staffName;
            set { _staffName = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum TableOrderStatus
    {
        Active,      // Order is being taken
        Sent,        // Order sent to kitchen
        Partial,     // Partially paid
        Paid,        // Fully paid
        Voided,      // Order voided
        Transferred  // Transferred to another table
    }

    public enum ItemSendStatus
    {
        NotSent,     // Not yet sent to kitchen
        Sent,        // Sent to kitchen
        Preparing,   // Being prepared
        Ready,       // Ready to serve
        Served       // Served to customer
    }

    public enum PaymentMethodType
    {
        Cash,
        Card,
        GiftCard
    }
}
