# Print Groups Quick Start Guide

## What Are Print Groups?

Print groups allow you to route menu items to specific printers automatically. For example:
- **Kitchen** group → Kitchen printer (mains, starters)
- **Bar** group → Bar printer (drinks, cocktails)
- **Grill** group → Grill station printer (grilled items)
- **Takeaway** group → Takeaway printer (delivery orders)
- **Label Printer** group → Brother QL-820NWB (component labels)

## Setup Process (3 Steps)

### Step 1: Add Your Printers
1. Go to **Settings** → **Printer Setup**
2. Click **+ Add Printer**
3. Fill in:
   - Printer Name: "Kitchen Printer 1"
   - IP Address: "192.168.1.100"
   - Port: 9100
   - Brand: EPSON / STAR / OTHER
   - Type: Kitchen / Bar / Receipt / etc.

### Step 2: Assign Printer to Group
1. In the same printer form, scroll down to **"Print Group"**
2. Click **"Select Print Group..."**
3. Choose:
   - **Existing group** (Kitchen, Bar, Grill, Takeaway, Label Printer)
   - **OR** click **"+ Create New Group"** to make a custom one
4. Click **Save Printer**

### Step 3: Configure Menu Items
1. Go to **Management** → **Menu Management**
2. Select Category → Edit Menu Item
3. Scroll to **"Print Settings"**
4. Click **"Select Print Group"** button
5. Choose the group (e.g., Kitchen)
6. Save the item

**That's it!** When someone orders that item, it will automatically print to the assigned printer.

## Component Labels (Optional)

Component labels print each part of a dish as a separate label (useful for complex meals).

### How to Enable:
1. Edit menu item → **Print Settings** section
2. Toggle **"Print Component Labels"** ON
3. Dialog appears → Enter component name (e.g., "Roti")
4. Click **"Add Component"**
5. Repeat for all components: "Curry", "Rice", "Salad", etc.
6. Each component will print as a separate label

### Example:
Item: **Chicken Tikka Masala Meal**
Components:
- Chicken Tikka
- Masala Sauce
- Basmati Rice
- Naan Bread
- Salad

**Result**: 5 separate labels printed on the label printer

## Managing Print Groups

### View All Groups
Go to **Settings** → **Print Groups**

Shows:
- Group name
- Assigned printer (if any)
- Status (Active/Disabled)
- Color code

### Create New Group
**Only from Printer Setup!**

1. **Settings** → **Printer Setup** → **Add/Edit Printer**
2. In **Print Group** section → **Select Print Group...**
3. Click **"+ Create New Group"**
4. Enter name: "Dessert Station"
5. Pick a color
6. Click **Create Group**

### Edit/Delete Groups
Use the **Print Groups** page under Settings.

## Workflow Example

### Scenario: Setting up a new kitchen printer for starters

1. **Add Printer**
   - Name: "Kitchen Starter Printer"
   - IP: 192.168.1.101
   - Port: 9100
   - Brand: EPSON
   - Type: Kitchen

2. **Assign to Group**
   - Print Group: **Kitchen** (select from list)
   - Save Printer

3. **Configure Menu Items**
   - Edit "Garlic Bread" → Print Group: Kitchen
   - Edit "Spring Rolls" → Print Group: Kitchen
   - Edit "Soup of the Day" → Print Group: Kitchen

4. **Test**
   - Place order with Garlic Bread
   - Order automatically prints to Kitchen Starter Printer!

## Common Questions

**Q: Can I assign multiple printers to one group?**
A: Currently, one printer per group. Future update will support multiple.

**Q: Can I create print groups from the menu item editor?**
A: No. Print groups are created ONLY in Printer Setup for centralized management.

**Q: What if I don't assign a print group to an item?**
A: It will print to the default receipt printer.

**Q: Can I print component labels without a print group?**
A: Yes! Component labels and print groups work independently.

**Q: How do I unassign a printer from a group?**
A: Edit the printer → Print Group → Select **"None"** → Save

**Q: Can I disable a print group without deleting it?**
A: Yes! Go to Print Groups page → Toggle the group off.

## Troubleshooting

**Printer not printing**
- Check printer is on and connected to network
- Verify IP address is correct in Printer Setup
- Test connection using "Test Connection" button

**Menu item not routing to correct printer**
- Verify menu item has print group assigned
- Check printer is assigned to that print group
- Ensure print group is Active (not disabled)

**Component labels not printing**
- Verify "Print Component Labels" toggle is ON
- Check components are listed below the toggle
- Ensure label printer is assigned to "Label Printer" group

## Default Print Groups

Your system comes with 5 pre-configured groups:

| Group | Color | Typical Use |
|-------|-------|-------------|
| Kitchen | Red | Mains, starters, hot food |
| Bar | Purple | Drinks, cocktails, beverages |
| Grill | Orange | Grilled items, BBQ |
| Takeaway | Blue | Delivery orders, takeaway |
| Label Printer | Green | Component labels, food labels |

You can create unlimited additional groups!

## Tips

- Use descriptive printer names: "Kitchen Main", "Kitchen Starter", "Bar Upstairs"
- Assign colors to groups that match your physical printer labels
- Test print after setup to verify routing works
- Review print groups monthly to remove unused ones
- Keep component labels short (fits on label better)

## Need Help?

Check the full documentation: `PRINT_GROUP_IMPLEMENTATION.md`
