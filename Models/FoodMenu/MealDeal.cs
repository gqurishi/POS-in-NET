using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace MyFirstMauiApp.Models.FoodMenu
{
    /// <summary>
    /// Represents a meal deal with bundled pricing and selection rules
    /// </summary>
    public class MealDeal : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string? _description;
        private decimal _price;
        private string _color = "#8B5CF6";
        private List<MealDealCategory> _categories = new();
        private bool _active = true;
        private int _displayOrder;
        private DateTime _createdAt;
        private DateTime _updatedAt;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string? Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Fixed bundle price
        /// </summary>
        public decimal Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); }
        }

        public string Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// List of selection categories with rules (stored as JSON in database)
        /// </summary>
        public List<MealDealCategory> Categories
        {
            get => _categories;
            set { _categories = value; OnPropertyChanged(); }
        }

        public bool Active
        {
            get => _active;
            set { _active = value; OnPropertyChanged(); }
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

        // JSON serialization helpers
        public string CategoriesJson => JsonSerializer.Serialize(Categories);

        public static List<MealDealCategory> ParseCategories(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<MealDealCategory>();

            try
            {
                return JsonSerializer.Deserialize<List<MealDealCategory>>(json) ?? new List<MealDealCategory>();
            }
            catch
            {
                return new List<MealDealCategory>();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a selection category within a meal deal
    /// (e.g., "Choose Your Main" - min 1, max 1, required)
    /// </summary>
    public class MealDealCategory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// "required" or "optional"
        /// </summary>
        public string SelectionType { get; set; } = "required";
        
        public int MinSelections { get; set; }
        public int MaxSelections { get; set; }
        
        /// <summary>
        /// List of menu item IDs available for this selection
        /// </summary>
        public List<string> MenuItemIds { get; set; } = new();

        // Helper properties
        public bool IsRequired => SelectionType == "required";
        public bool IsOptional => SelectionType == "optional";
    }
}
