using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyFirstMauiApp.Models.FoodMenu
{
    /// <summary>
    /// Represents a menu category with support for hierarchical structure (parent/sub-categories)
    /// </summary>
    public class MenuCategory : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string? _description;
        private string? _parentId;
        private int _displayOrder;
        private bool _active = true;
        private string _color = "#3B82F6";
        private string _icon = "🍽️";
        private DateTime _createdAt;
        private DateTime _updatedAt;
        private string? _parentCategoryName;
        private bool _isSelected;

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
        /// NULL for top-level categories, set to parent's Id for sub-categories
        /// </summary>
        public string? ParentId
        {
            get => _parentId;
            set { _parentId = value; OnPropertyChanged(); }
        }

        public int DisplayOrder
        {
            get => _displayOrder;
            set { _displayOrder = value; OnPropertyChanged(); }
        }

        public bool Active
        {
            get => _active;
            set { _active = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Hex color code for visual coding (e.g., "#3B82F6")
        /// </summary>
        public string Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Icon or emoji for display
        /// </summary>
        public string Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(); }
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

        /// <summary>
        /// Display name of parent category (for UI, not stored in database)
        /// </summary>
        public string? ParentCategoryName
        {
            get => _parentCategoryName;
            set { _parentCategoryName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// UI state for selection (not stored in database)
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        // Helper properties for UI
        public bool IsSubCategory => !string.IsNullOrEmpty(ParentId);
        public bool IsTopLevel => string.IsNullOrEmpty(ParentId);

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
