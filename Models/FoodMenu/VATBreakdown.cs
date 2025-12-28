using System.Collections.Generic;

namespace MyFirstMauiApp.Models.FoodMenu
{
    /// <summary>
    /// Represents the VAT breakdown for an item
    /// </summary>
    public class VATBreakdown
    {
        public decimal NetPrice { get; set; }
        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
        public decimal GrossPrice { get; set; }
        
        /// <summary>
        /// For mixed VAT items, breakdown by component
        /// </summary>
        public List<ComponentVATBreakdown>? Components { get; set; }
        
        public bool IsMixedVat => Components != null && Components.Count > 0;
    }

    /// <summary>
    /// VAT breakdown for a single component in mixed VAT items
    /// </summary>
    public class ComponentVATBreakdown
    {
        public string ComponentName { get; set; } = string.Empty;
        public decimal NetPrice { get; set; }
        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
        public decimal GrossPrice { get; set; }
    }
}
