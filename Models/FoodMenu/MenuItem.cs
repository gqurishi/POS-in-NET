using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Linq;

namespace MyFirstMauiApp.Models.FoodMenu
{
    /// <summary>
    /// Represents a menu item with pricing, VAT, addons, and relationships to comments/notes
    /// </summary>
    public class FoodMenuItem : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _categoryId = string.Empty;
        private string _name = string.Empty;
        private string? _description;
        private decimal _price;
        private string _color = "#3B82F6";
        private int _displayOrder;
        private bool _isFeatured;
        private int? _preparationTime;
        
        // Legacy VAT fields (kept for backwards compatibility)
        private decimal _vatRate = 20.00m;
        private string _vatType = "simple";
        private bool _isVatExempt;
        private string? _vatNotes;
        
        // New VAT system fields
        private string _vatConfigType = "standard"; // "standard" or "component"
        private string _vatCategory = "HotFood"; // NoVAT, HotFood, ColdFood, HotBeverage, ColdBeverage, Alcohol
        private decimal _calculatedVatRate = 20.00m; // Effective VAT rate for display
        private List<MenuItemComponent> _components = new(); // For meal deals
        
        private List<Addon> _addons = new();
        private List<string> _tags = new();
        private List<MenuItemQuickNote> _quickNotes = new(); // Quick notes for this item (max 6)
        private bool _printInRed;
        
        // Label Print Settings
        private string? _labelText; // Custom text for label printer (empty = no label)
        private bool _printComponentLabels; // Print component labels for meal deals
        
        // Print Group (Printer Routing)
        private string? _printGroupId; // Which printer group this item prints to
        
        private DateTime _createdAt;
        private DateTime _updatedAt;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string CategoryId
        {
            get => _categoryId;
            set { _categoryId = value; OnPropertyChanged(); }
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

        public decimal Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Hex color code for visual coding
        /// </summary>
        public string Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); }
        }

        public int DisplayOrder
        {
            get => _displayOrder;
            set { _displayOrder = value; OnPropertyChanged(); }
        }

        public bool IsFeatured
        {
            get => _isFeatured;
            set { _isFeatured = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Preparation time in minutes
        /// </summary>
        public int? PreparationTime
        {
            get => _preparationTime;
            set { _preparationTime = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// VAT rate percentage (e.g., 20.00 for 20%)
        /// </summary>
        public decimal VatRate
        {
            get => _vatRate;
            set { _vatRate = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// "simple" for single VAT rate, "mixed" for component-based VAT
        /// </summary>
        public string VatType
        {
            get => _vatType;
            set { _vatType = value; OnPropertyChanged(); }
        }

        public bool IsVatExempt
        {
            get => _isVatExempt;
            set { _isVatExempt = value; OnPropertyChanged(); }
        }

        public string? VatNotes
        {
            get => _vatNotes;
            set { _vatNotes = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// VAT configuration type: "standard" for simple items, "component" for meal deals
        /// </summary>
        public string VatConfigType
        {
            get => _vatConfigType;
            set { _vatConfigType = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// VAT category for standard items: NoVAT, HotFood, ColdFood, HotBeverage, ColdBeverage, Alcohol
        /// </summary>
        public string VatCategory
        {
            get => _vatCategory;
            set { _vatCategory = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Calculated effective VAT rate (auto-computed for component items)
        /// </summary>
        public decimal CalculatedVatRate
        {
            get => _calculatedVatRate;
            set { _calculatedVatRate = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// List of components for meal deals (empty for standard items)
        /// </summary>
        public List<MenuItemComponent> Components
        {
            get => _components;
            set { _components = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// List of addons/modifiers (stored as JSON in database)
        /// </summary>
        public List<Addon> Addons
        {
            get => _addons;
            set { _addons = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Tags for search/filtering (stored as JSON in database)
        /// </summary>
        public List<string> Tags
        {
            get => _tags;
            set { _tags = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Quick notes for this menu item (max 6)
        /// Stored in separate MenuItemQuickNotes table
        /// </summary>
        public List<MenuItemQuickNote> QuickNotes
        {
            get => _quickNotes;
            set { _quickNotes = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Print this item in red ink (two-color printers only)
        /// Stored in database as TINYINT (0/1)
        /// </summary>
        public bool PrintInRed
        {
            get => _printInRed;
            set { _printInRed = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Custom text to print on label (empty = no label printing)
        /// </summary>
        public string? LabelText
        {
            get => _labelText;
            set { _labelText = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Print individual labels for each component (meal deals only)
        /// When true, prints ONLY component labels, not main item label
        /// </summary>
        public bool PrintComponentLabels
        {
            get => _printComponentLabels;
            set { _printComponentLabels = value; OnPropertyChanged(); }
        }

        private string? _componentLabelsJson;
        /// <summary>
        /// JSON array of component names to print as labels
        /// </summary>
        public string? ComponentLabelsJson
        {
            get => _componentLabelsJson;
            set { _componentLabelsJson = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Print group ID - determines which printer this item routes to
        /// </summary>
        public string? PrintGroupId
        {
            get => _printGroupId;
            set { _printGroupId = value; OnPropertyChanged(); }
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
        public bool HasAddons => Addons.Count > 0;
        public bool HasQuickNotes => QuickNotes.Count > 0;
        public int QuickNotesCount => QuickNotes.Count;
        public bool CanAddMoreQuickNotes => QuickNotes.Count < 6;
        public bool IsMixedVat => VatType == "mixed";
        public bool IsSimpleVat => VatType == "simple";
        
        // New VAT helper properties
        public bool IsStandardItem => VatConfigType == "standard";
        public bool IsComponentItem => VatConfigType == "component";
        public bool HasComponents => Components.Count > 0;
        public bool IsNoVat => VatCategory == "NoVAT";
        public bool IsHotFood => VatCategory == "HotFood";
        public bool IsColdFood => VatCategory == "ColdFood";
        public bool IsHotBeverage => VatCategory == "HotBeverage";
        public bool IsColdBeverage => VatCategory == "ColdBeverage";
        public bool IsAlcohol => VatCategory == "Alcohol";

        // JSON serialization helpers
        public string AddonsJson => JsonSerializer.Serialize(Addons);
        public string TagsJson => JsonSerializer.Serialize(Tags);

        public static List<Addon> ParseAddons(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<Addon>();

            try
            {
                return JsonSerializer.Deserialize<List<Addon>>(json) ?? new List<Addon>();
            }
            catch
            {
                return new List<Addon>();
            }
        }

        public static List<string> ParseTags(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
