using System;
using System.Collections.Generic;
using System.Linq;
using MyFirstMauiApp.Models.FoodMenu;

namespace MyFirstMauiApp.Services
{
    /// <summary>
    /// Service for calculating VAT (Value Added Tax) for menu items
    /// Supports UK VAT rates: 20% (standard), 5% (reduced), 0% (zero-rated/exempt)
    /// </summary>
    public class VATCalculationService
    {
        // UK VAT rates
        public const decimal STANDARD_RATE = 20.00m;
        public const decimal REDUCED_RATE = 5.00m;
        public const decimal ZERO_RATE = 0.00m;

        /// <summary>
        /// Calculate VAT for a simple VAT item (single rate applied to full price)
        /// </summary>
        public VATBreakdown CalculateSimpleVAT(FoodMenuItem item)
        {
            if (item.IsVatExempt)
            {
                return new VATBreakdown
                {
                    NetPrice = item.Price,
                    VatRate = 0.00m,
                    VatAmount = 0.00m,
                    GrossPrice = item.Price
                };
            }

            var netPrice = item.Price;
            var vatAmount = (netPrice * item.VatRate) / 100;
            var grossPrice = netPrice + vatAmount;

            return new VATBreakdown
            {
                NetPrice = Math.Round(netPrice, 2),
                VatRate = item.VatRate,
                VatAmount = Math.Round(vatAmount, 2),
                GrossPrice = Math.Round(grossPrice, 2)
            };
        }

        /// <summary>
        /// Calculate VAT for a mixed VAT item (different rates per component)
        /// </summary>
        public VATBreakdown CalculateMixedVAT(FoodMenuItem item, List<ItemComponent> components)
        {
            if (!components.Any())
            {
                // Fallback to simple VAT if no components
                return CalculateSimpleVAT(item);
            }

            var componentBreakdowns = new List<ComponentVATBreakdown>();
            decimal totalVAT = 0;
            decimal totalNet = 0;

            foreach (var component in components)
            {
                var compNetPrice = component.ComponentCost;
                var compVatAmount = (compNetPrice * component.VatRate) / 100;
                var compGrossPrice = compNetPrice + compVatAmount;

                componentBreakdowns.Add(new ComponentVATBreakdown
                {
                    ComponentName = component.ComponentName,
                    NetPrice = Math.Round(compNetPrice, 2),
                    VatRate = component.VatRate,
                    VatAmount = Math.Round(compVatAmount, 2),
                    GrossPrice = Math.Round(compGrossPrice, 2)
                });

                totalVAT += compVatAmount;
                totalNet += compNetPrice;
            }

            return new VATBreakdown
            {
                NetPrice = Math.Round(totalNet, 2),
                VatRate = 0, // Not applicable for mixed VAT
                VatAmount = Math.Round(totalVAT, 2),
                GrossPrice = Math.Round(totalNet + totalVAT, 2),
                Components = componentBreakdowns
            };
        }

        /// <summary>
        /// Calculate VAT including addons
        /// </summary>
        public VATBreakdown CalculateWithAddons(FoodMenuItem item, List<Addon> selectedAddons, List<ItemComponent>? components = null)
        {
            VATBreakdown baseBreakdown;

            // Calculate base item VAT
            if (item.IsMixedVat && components != null && components.Any())
            {
                baseBreakdown = CalculateMixedVAT(item, components);
            }
            else
            {
                baseBreakdown = CalculateSimpleVAT(item);
            }

            // If no addons, return base breakdown
            if (selectedAddons == null || !selectedAddons.Any())
            {
                return baseBreakdown;
            }

            // Calculate addon VAT (addons typically use same rate as base item)
            decimal addonsNet = selectedAddons.Sum(a => a.Price);
            decimal addonsVat = (addonsNet * item.VatRate) / 100;

            return new VATBreakdown
            {
                NetPrice = Math.Round(baseBreakdown.NetPrice + addonsNet, 2),
                VatRate = item.VatRate,
                VatAmount = Math.Round(baseBreakdown.VatAmount + addonsVat, 2),
                GrossPrice = Math.Round(baseBreakdown.GrossPrice + addonsNet + addonsVat, 2),
                Components = baseBreakdown.Components // Preserve component breakdown if mixed VAT
            };
        }

        /// <summary>
        /// Calculate reverse VAT (extract VAT from gross price)
        /// Useful for price-inclusive scenarios
        /// </summary>
        public VATBreakdown CalculateReverseVAT(decimal grossPrice, decimal vatRate)
        {
            // Formula: Net = Gross / (1 + VAT%)
            var netPrice = grossPrice / (1 + (vatRate / 100));
            var vatAmount = grossPrice - netPrice;

            return new VATBreakdown
            {
                NetPrice = Math.Round(netPrice, 2),
                VatRate = vatRate,
                VatAmount = Math.Round(vatAmount, 2),
                GrossPrice = Math.Round(grossPrice, 2)
            };
        }

        /// <summary>
        /// Get VAT summary for multiple items (for receipts/reports)
        /// Groups by VAT rate
        /// </summary>
        public Dictionary<decimal, VATSummary> GetVATSummary(List<VATBreakdown> breakdowns)
        {
            var summary = new Dictionary<decimal, VATSummary>();

            foreach (var breakdown in breakdowns)
            {
                if (breakdown.IsMixedVat && breakdown.Components != null)
                {
                    // For mixed VAT, add each component separately
                    foreach (var component in breakdown.Components)
                    {
                        if (!summary.ContainsKey(component.VatRate))
                        {
                            summary[component.VatRate] = new VATSummary
                            {
                                VatRate = component.VatRate,
                                TotalNet = 0,
                                TotalVAT = 0,
                                TotalGross = 0
                            };
                        }

                        summary[component.VatRate].TotalNet += component.NetPrice;
                        summary[component.VatRate].TotalVAT += component.VatAmount;
                        summary[component.VatRate].TotalGross += component.GrossPrice;
                    }
                }
                else
                {
                    // Simple VAT
                    if (!summary.ContainsKey(breakdown.VatRate))
                    {
                        summary[breakdown.VatRate] = new VATSummary
                        {
                            VatRate = breakdown.VatRate,
                            TotalNet = 0,
                            TotalVAT = 0,
                            TotalGross = 0
                        };
                    }

                    summary[breakdown.VatRate].TotalNet += breakdown.NetPrice;
                    summary[breakdown.VatRate].TotalVAT += breakdown.VatAmount;
                    summary[breakdown.VatRate].TotalGross += breakdown.GrossPrice;
                }
            }

            // Round all summaries
            foreach (var kvp in summary)
            {
                kvp.Value.TotalNet = Math.Round(kvp.Value.TotalNet, 2);
                kvp.Value.TotalVAT = Math.Round(kvp.Value.TotalVAT, 2);
                kvp.Value.TotalGross = Math.Round(kvp.Value.TotalGross, 2);
            }

            return summary;
        }

        /// <summary>
        /// Validate that component costs match item price
        /// </summary>
        public bool ValidateComponents(decimal itemPrice, List<ItemComponent> components)
        {
            if (components == null || !components.Any())
                return false;

            var totalComponentCost = components.Sum(c => c.ComponentCost);
            var difference = Math.Abs(itemPrice - totalComponentCost);

            // Allow for small rounding differences (1 penny)
            return difference <= 0.01m;
        }

        /// <summary>
        /// Format VAT breakdown for display (e.g., on receipts)
        /// </summary>
        public string FormatVATBreakdown(VATBreakdown breakdown)
        {
            if (breakdown.IsMixedVat && breakdown.Components != null)
            {
                var lines = new List<string>
                {
                    "VAT Breakdown (Mixed):"
                };

                foreach (var component in breakdown.Components)
                {
                    lines.Add($"  {component.ComponentName}: £{component.NetPrice:F2} + VAT({component.VatRate}%) £{component.VatAmount:F2} = £{component.GrossPrice:F2}");
                }

                lines.Add($"Total: £{breakdown.NetPrice:F2} + VAT £{breakdown.VatAmount:F2} = £{breakdown.GrossPrice:F2}");

                return string.Join("\n", lines);
            }
            else
            {
                return $"Net: £{breakdown.NetPrice:F2} + VAT({breakdown.VatRate}%) £{breakdown.VatAmount:F2} = £{breakdown.GrossPrice:F2}";
            }
        }
    }

    /// <summary>
    /// VAT summary for reporting
    /// </summary>
    public class VATSummary
    {
        public decimal VatRate { get; set; }
        public decimal TotalNet { get; set; }
        public decimal TotalVAT { get; set; }
        public decimal TotalGross { get; set; }
    }
}
