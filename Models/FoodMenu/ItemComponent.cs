using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyFirstMauiApp.Models.FoodMenu
{
    /// <summary>
    /// Represents a component of a menu item for mixed VAT calculation
    /// (e.g., "Food portion" with 20% VAT, "Toy" with 0% VAT in a Kids Meal)
    /// </summary>
    public class ItemComponent : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _menuItemId = string.Empty;
        private string _componentName = string.Empty;
        private decimal _componentCost;
        private decimal _vatRate;
        private string _componentType = "food";
        private int _displayOrder;
        private DateTime _createdAt;
        private DateTime _updatedAt;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string MenuItemId
        {
            get => _menuItemId;
            set { _menuItemId = value; OnPropertyChanged(); }
        }

        public string ComponentName
        {
            get => _componentName;
            set { _componentName = value; OnPropertyChanged(); }
        }

        public decimal ComponentCost
        {
            get => _componentCost;
            set { _componentCost = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// VAT rate for this specific component (e.g., 20.00, 5.00, 0.00)
        /// </summary>
        public decimal VatRate
        {
            get => _vatRate;
            set { _vatRate = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Type: "food", "non-food", "beverage", etc.
        /// </summary>
        public string ComponentType
        {
            get => _componentType;
            set { _componentType = value; OnPropertyChanged(); }
        }

        public int DisplayOrder
        {
            get => _displayOrder;
            set { _displayOrder = value; OnPropertyChanged(); }
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

        // Helper properties for VAT calculation
        public decimal VatAmount => (ComponentCost * VatRate) / 100;
        public decimal TotalCost => ComponentCost + VatAmount;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
