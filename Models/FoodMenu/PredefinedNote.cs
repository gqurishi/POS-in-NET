using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyFirstMauiApp.Models.FoodMenu
{
    /// <summary>
    /// Represents a predefined kitchen note (e.g., "Cook well done", "Nut allergy - URGENT")
    /// </summary>
    public class PredefinedNote : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _noteText = string.Empty;
        private string? _category;
        private string _priority = "normal";
        private int _displayOrder;
        private bool _active = true;
        private string _color = "#F59E0B";
        private DateTime _createdAt;
        private DateTime _updatedAt;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string NoteText
        {
            get => _noteText;
            set { _noteText = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Category: "Cooking", "Allergy", "Special", etc.
        /// </summary>
        public string? Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Priority: "low", "normal", "high", "urgent"
        /// </summary>
        public string Priority
        {
            get => _priority;
            set { _priority = value; OnPropertyChanged(); }
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
        /// Hex color code (often based on priority: red for urgent, yellow for normal)
        /// </summary>
        public string Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); }
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
        public bool IsUrgent => Priority == "urgent";
        public bool IsHigh => Priority == "high";
        public bool IsNormal => Priority == "normal";
        public bool IsLow => Priority == "low";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
