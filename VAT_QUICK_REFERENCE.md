# Quick Reference - VAT System Usage

## For Restaurant Staff

### Creating Menu Items

#### Standard Item (Single Item)
1. Open menu management
2. Create/edit item
3. Select "Standard Item" radio button
4. Choose VAT category from dropdown:
   - **Hot Food** - Cooked meals (20% takeaway, 20% table)
   - **Cold Food** - Salads, sandwiches (0% takeaway, 20% table)
   - **Hot Beverage** - Coffee, tea (20% all)
   - **Cold Beverage** - Soda, water (0% takeaway, 20% table)
   - **Alcohol** - Beer, wine (20% all)
   - **No VAT** - Special exempt items
5. Save item

#### Meal Deal (Multi-Component)
1. Create/edit item
2. Select "Meal Deal with Components" radio button
3. Click "+ Add Component"
4. For each component:
   - Name: "Burger", "Chips", "Drink", etc.
   - Price: Individual component price
   - Type: Choose hot/cold category
5. Verify total = sum of components
6. Check VAT breakdown display
7. Save item

### Taking Table Orders
1. Open table order screen
2. Add items to order
3. System automatically calculates:
   - Subtotal (sum of item prices)
   - **VAT (20% of subtotal)** ← Always 20% for tables
   - Service charge (if applied)
   - Total
4. VAT shown separately in totals panel

### Viewing Online Orders
- Online orders (takeaway/delivery) show pre-calculated tax
- Tax calculated by OrderWeb server using smart VAT rules
- POS displays tax as received from server

---

## For Developers

### VAT Calculation Quick Reference

```csharp
// Get VAT for a menu item
using MyFirstMauiApp.Services;

decimal vatRate = VATCalculator.CalculateVatRate(item, "Table");
// Returns: 20.00 (always for table orders)

decimal vatAmount = VATCalculator.CalculateVatAmount(item, "Takeaway", quantity: 2);
// Returns: Currency amount based on smart calculation

string description = VATCalculator.GetVatDescription(item, "Delivery");
// Returns: "20% VAT (hot food)" or "0% VAT (cold food)" etc.
```

### Table Order VAT Example

```csharp
var order = new TableOrder 
{
    TableNumber = 5,
    CoverCount = 2
};

// Add items
order.Items.Add(new TableOrderItem 
{
    Name = "Burger",
    UnitPrice = 8.50m,
    Quantity = 2
});

// Recalculate
order.RecalculateAll();

// Results:
// order.Subtotal = 17.00
// order.VAT = 3.40 (20% of 17.00)
// order.Total = 20.40
```

### Database Queries

```sql
-- Get all meal deals
SELECT * FROM FoodMenuItems 
WHERE vat_config_type = 'ComponentBased';

-- Get components for a meal deal
SELECT c.* 
FROM MenuItemComponents c
WHERE c.MenuItemId = 'YOUR_ITEM_ID'
ORDER BY c.SortOrder;

-- Calculate VAT for an order
SELECT 
    o.OrderId,
    o.SubtotalAmount as Subtotal,
    o.TaxAmount as VAT,
    o.TotalAmount as Total,
    (o.TaxAmount / o.SubtotalAmount * 100) as VATPercentage
FROM Orders o;
```

---

## Troubleshooting

### VAT Not Calculating
**Problem:** VAT shows £0.00 on table order  
**Solution:** Call `order.RecalculateAll()` after modifying items

### Wrong VAT Amount
**Problem:** Expected 20% but getting different amount  
**Solution:** Check if discount is applied - VAT calculated on subtotal, total includes discount

### Meal Deal VAT Error
**Problem:** Component VAT doesn't add up  
**Solution:** Ensure component prices sum to item price exactly

### Build Error: VAT Field Not Found
**Problem:** Code references VAT property but throws error  
**Solution:** Run database migration `vat_system_migration.sql` first

---

## API Reference

### VATCalculator Methods

| Method | Parameters | Returns | Purpose |
|--------|-----------|---------|---------|
| `CalculateVatRate` | item, orderType | decimal (0-20) | Get VAT percentage |
| `CalculateVatAmount` | item, orderType, qty | decimal (£) | Get VAT in currency |
| `GetComponentVatBreakdown` | item, orderType | Dictionary | VAT per component |
| `GetVatDescription` | item, orderType | string | Display text |

### TableOrder Properties

| Property | Type | Description |
|----------|------|-------------|
| `Subtotal` | decimal | Sum of item prices |
| `VAT` | decimal | 20% of subtotal (table orders) |
| `ServiceCharge` | decimal | Optional service charge |
| `Discount` | decimal | Applied discounts |
| `Total` | decimal | Subtotal + VAT + Service - Discount |

### TableOrder Methods

| Method | Purpose |
|--------|---------|
| `CalculateSubtotal()` | Sum all item prices |
| `CalculateServiceCharge()` | Apply service % |
| `CalculateTotal()` | Calculate VAT and final total |
| `RecalculateAll()` | Recalc everything in order |

---

## Configuration Files

### Database Migration
**File:** `Database/vat_system_migration.sql`  
**Purpose:** Adds VAT columns to existing database  
**Run Once:** Yes, when upgrading existing system

### VAT Calculator
**File:** `Services/VATCalculator.cs`  
**Configuration:** UK VAT rates hardcoded (20% standard, 0% zero-rated)  
**Customization:** Edit rate constants if VAT rates change

### Menu Item Model
**File:** `Models/FoodMenu/MenuItem.cs`  
**VAT Properties:** VatConfigType, VatCategory, CalculatedVatRate, Components

---

## Testing Checklist

### Before Release
- [ ] Create standard hot item → Check VAT = 20%
- [ ] Create standard cold item → Check VAT category saved
- [ ] Create meal deal → Check components save
- [ ] Open table order → Check VAT displays
- [ ] Add items to table → Check VAT updates
- [ ] Apply service charge → Check VAT unaffected
- [ ] Apply discount → Check VAT on subtotal, not discounted total
- [ ] Process payment → Check VAT in receipt
- [ ] View online order → Check tax displays
- [ ] Run build → Check 0 errors

### After Deployment
- [ ] Verify database migration successful
- [ ] Check existing items have VAT defaults
- [ ] Test creating new items
- [ ] Test editing existing items
- [ ] Monitor VAT calculations on live orders

---

## Support Contacts

**Technical Issues:** Development team  
**VAT Compliance Questions:** UK tax advisor  
**Database Issues:** DBA team  
**UI/UX Feedback:** Product team

---

**Last Updated:** 2025  
**Version:** 1.0  
**Status:** Production Ready ✅
