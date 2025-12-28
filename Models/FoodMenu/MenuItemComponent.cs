using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyFirstMauiApp.Models.FoodMenu
{
    /// <summary>
    /// Represents a component of a meal deal with its own VAT rate
    /// Used for items with mixed hot/cold components (e.g., Biryani + Raita)
    /// </summary>
    public class MenuItemComponent : INotifyPropertyChanged
    {
        private int _id;
        private string _menuItemId = string.Empty;
        private string _componentName = string.Empty;
        private decimal _componentPrice;
        private string _componentType = "HotFood";
        private decimal _vatRate = 20.00m;
        private int _sortOrder;
        private DateTime _createdAt;
        private DateTime _updatedAt;

        /// <summary>
        /// Component ID (auto-increment)
        /// </summary>
        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Parent menu item ID
        /// </summary>
        public string MenuItemId
        {
            get => _menuItemId;
            set { _menuItemId = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Name of the component (e.g., "Chicken Biryani", "Raita")
        /// </summary>
        public string ComponentName
        {
            get => _componentName;
            set { _componentName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Price of this component
        /// </summary>
        public decimal ComponentPrice
        {
            get => _componentPrice;
            set { _componentPrice = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Type: HotFood, ColdFood, HotBeverage, ColdBeverage, Alcohol
        /// Determines VAT rate for takeaway orders
        /// </summary>
        public string ComponentType
        {
            get => _componentType;
            set 
            { 
                _componentType = value;
                UpdateVatRateBasedOnType();
                OnPropertyChanged(); 
            }
        }

        /// <summary>
        /// VAT rate for this component (auto-set based on ComponentType)
        /// 20% for hot items and alcohol, 0% for cold items (takeaway)
        /// </summary>
        public decimal VatRate
        {
            get => _vatRate;
            set { _vatRate = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Display order in the component list
        /// </summary>
        public int SortOrder
        {
            get => _sortOrder;
            set { _sortOrder = value; OnPropertyChanged(); }
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

        // Helper properties
        public bool IsHot => ComponentType == "HotFood" || ComponentType == "HotBeverage";
        public bool IsCold => ComponentType == "ColdFood" || ComponentType == "ColdBeverage";
        public bool IsAlcohol => ComponentType == "Alcohol";

        /// <summary>
        /// Display label for component type
        /// </summary>
        public string ComponentTypeDisplay => ComponentType switch
        {
            "HotFood" => "Hot Food",
            "ColdFood" => "Cold Food",
            "HotBeverage" => "Hot Beverage",
            "ColdBeverage" => "Cold Beverage",
            "Alcohol" => "Alcohol",
            _ => ComponentType
        };

        /// <summary>
        /// Auto-update VAT rate when component type changes
        /// Hot items and alcohol = 20%, Cold items = 0% (for takeaway)
        /// </summary>
        private void UpdateVatRateBasedOnType()
        {
            VatRate = ComponentType switch
            {
                "ColdFood" => 0m,
                "ColdBeverage" => 0m,
                "HotFood" => 20m,
                "HotBeverage" => 20m,
                "Alcohol" => 20m,
                _ => 20m
            };
        }

        /// <summary>
        /// Calculate VAT amount for this component
        /// </summary>
        /// <param name="orderType">Table, Takeaway, Collection, or Delivery</param>
        /// <returns>VAT amount in currency</returns>
        public decimal CalculateVatAmount(string orderType)
        {
            // Table orders = always 20%
            if (orderType == "Table" || orderType == "DineIn")
            {
                return ComponentPrice * 0.20m;
            }

            // Takeaway/Collection/Delivery = use component's VAT rate
            return ComponentPrice * (VatRate / 100m);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
