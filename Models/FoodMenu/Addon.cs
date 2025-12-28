using System;

namespace MyFirstMauiApp.Models.FoodMenu
{
    /// <summary>
    /// Represents an addon/modifier for a menu item (e.g., "Extra Cheese - £2.00")
    /// </summary>
    public class Addon
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool Available { get; set; } = true;
    }
}
