using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyFirstMauiApp.Models.FoodMenu
{
    /// <summary>
    /// Represents a quick note specific to a menu item (max 6 per item)
    /// Used for rapid order customization without typing
    /// </summary>
    public class MenuItemQuickNote : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _menuItemId = string.Empty;
        private string _noteText = string.Empty;
        private int _displayOrder;
        private bool _active = true;
        private bool _isSelected; // For UI multi-select
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

        public string NoteText
        {
            get => _noteText;
            set { _noteText = value; OnPropertyChanged(); }
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
        /// Used for multi-select UI when adding notes to an order
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
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

        // UI Helper Properties
        public Color DisplayColor => IsSelected ? Color.FromArgb("#6366F1") : Color.FromArgb("#E2E8F0");
        public Color TextColor => IsSelected ? Colors.White : Color.FromArgb("#475569");
        public Color BorderColor => IsSelected ? Color.FromArgb("#6366F1") : Color.FromArgb("#CBD5E1");

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
