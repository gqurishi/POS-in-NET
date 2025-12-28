using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyFirstMauiApp.Models.FoodMenu
{
    /// <summary>
    /// Represents a predefined customer-facing comment (e.g., "No onions", "Extra spicy")
    /// </summary>
    public class PredefinedComment : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _commentText = string.Empty;
        private string? _category;
        private int _displayOrder;
        private bool _active = true;
        private string _color = "#10B981";
        private DateTime _createdAt;
        private DateTime _updatedAt;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string CommentText
        {
            get => _commentText;
            set { _commentText = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Category: "Dietary", "Preparation", "Allergies", etc.
        /// </summary>
        public string? Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); }
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
        /// Hex color code for visual categorization
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
