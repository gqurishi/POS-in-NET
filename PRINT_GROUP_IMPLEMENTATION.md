# Print Group System - Complete Implementation

## Overview
This document describes the complete Print Group system implementation that enables multi-printer routing for menu items through centralized printer group management.

## System Architecture

### Print Group Management Strategy
**CENTRALIZED CONTROL**: Print groups are managed exclusively in the **Printer Setup** page.
- ✅ **Create** print groups in Printer Setup
- ✅ **Assign** printers to groups in Printer Setup
- ✅ **Select** from existing groups in menu item editor (read-only)

### Database Schema

#### 1. `print_groups` Table
```sql
CREATE TABLE print_groups (
    id VARCHAR(36) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    printer_ip VARCHAR(45) NULL,
    printer_port INT NULL,
    printer_type VARCHAR(20) NULL,
    is_active BOOLEAN DEFAULT TRUE,
    color_code VARCHAR(7) DEFAULT '#6366F1',
    display_order INT DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

**Default Groups**:
- Kitchen (#EF4444 - Red)
- Bar (#8B5CF6 - Purple)
- Grill (#F97316 - Orange)
- Takeaway (#3B82F6 - Blue)
- Label Printer (#22C55E - Green)

#### 2. `network_printers` Table
```sql
-- Added column
print_group_id VARCHAR(36) NULL
```
Links a printer to a print group for menu item routing.

#### 3. `FoodMenuItems` Table
```sql
-- Existing columns
component_labels_json TEXT NULL,      -- JSON array of component names
print_group_id VARCHAR(36) NULL       -- References print_groups.id
```

### Models

#### PrintGroup.cs
```csharp
namespace MyFirstMauiApp.Models;

public class PrintGroup : INotifyPropertyChanged
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? PrinterIp { get; set; }
    public int? PrinterPort { get; set; }
    public string? PrinterType { get; set; }
    public bool IsActive { get; set; }
    public string? ColorCode { get; set; }
    public int DisplayOrder { get; set; }
    
    // Computed properties
    public bool HasPrinter => !string.IsNullOrEmpty(PrinterIp);
    public string StatusText => IsActive ? "Active" : "Disabled";
    public string PrinterInfo => HasPrinter ? $"{PrinterIp}:{PrinterPort}" : "No printer assigned";
}
```

#### NetworkPrinter.cs
```csharp
// Added property
public string? PrintGroupId { get; set; }
```

#### FoodMenuItem.cs
```csharp
// Existing properties
public string? ComponentLabelsJson { get; set; }
public string? PrintGroupId { get; set; }

// Helper methods
public List<string> GetComponentLabels()
{
    if (string.IsNullOrWhiteSpace(ComponentLabelsJson))
        return new List<string>();
    
    return JsonSerializer.Deserialize<List<string>>(ComponentLabelsJson) ?? new List<string>();
}
```

### Services

#### PrintGroupService.cs (`MyFirstMauiApp.Services`)
Complete CRUD operations for print groups:
- `GetAllPrintGroupsAsync()` - Fetch all groups
- `CreatePrintGroupAsync(PrintGroup)` - Create new group
- `UpdatePrintGroupAsync(PrintGroup)` - Update existing
- `DeletePrintGroupAsync(string id)` - Delete group
- `TogglePrintGroupAsync(string id)` - Enable/disable

#### NetworkPrinterDatabaseService.cs
Updated to handle `print_group_id`:
- CREATE TABLE includes `print_group_id`
- INSERT query includes `print_group_id`
- UPDATE query includes `print_group_id`
- MapPrinter reads `print_group_id`

#### MenuItemService.cs
Handles both `component_labels_json` and `print_group_id`:
- SELECT queries include both fields
- INSERT includes component labels and print group
- UPDATE modifies both fields
- ParseMenuItem deserializes JSON and reads group ID

### User Interface

#### 1. Printer Setup Page (`PrinterSetupPage.xaml`)

**Print Group Assignment Field**:
```xaml
<VerticalStackLayout Spacing="8">
    <Label Text="Print Group"/>
    <Label Text="Assign this printer to a print group for menu item routing"/>
    <Button x:Name="PrintGroupButton"
            Text="Select Print Group..."
            Clicked="OnSelectPrintGroupClicked"/>
</VerticalStackLayout>
```

**Features**:
- Shows print group selector dialog
- Allows creating new print groups
- Saves `print_group_id` when printer is saved
- Updates button text when group selected

#### 2. Print Group Selector Dialog (`PrintGroupSelectorDialog.xaml`)

**Layout**:
- Dark header (#0F172A)
- Scrollable list of print groups
- "None" option to unassign
- Color-coded group indicators
- Warning icon for groups with assigned printers
- "+ Create New Group" button

**Returns**:
```csharp
(string? groupId, string? action)
// action: "select" | "create" | "cancel"
```

#### 3. Create Print Group Dialog (`CreatePrintGroupDialog.xaml`)

**Features**:
- Green header (#22C55E)
- Group name input field
- 10 color options (selectable circles)
- Creates group with GUID
- Returns to selector after creation

**Returns**:
```csharp
(string? name, string? color)
```

#### 4. Menu Item Editor (`AddEditItemPage.xaml`)

**Print Settings Section**:
- Print Component Labels toggle
- Component labels list (add/remove)
- Print Group button (read-only selection)
- Uses `PrintGroupDialog` to select

### Workflow

#### Setting Up Print Groups

1. **Navigate to Printer Setup**
   - Settings → Printer Setup

2. **Add/Edit Printer**
   - Fill in printer details (Name, IP, Port, Brand, Type)
   - Scroll to "Print Group" section
   - Click "Select Print Group..."

3. **Assign to Existing Group**
   - Select group from list
   - Click outside or tap group
   - Group assigned, button shows "✓ Print Group Assigned"

4. **Create New Group**
   - Click "+ Create New Group"
   - Enter group name (e.g., "Dessert Station")
   - Select color
   - Click "Create Group"
   - Group created and automatically assigned

5. **Save Printer**
   - Click "Save Printer" or "Update Printer"
   - Printer saved with `print_group_id`

#### Using Print Groups in Menu Items

1. **Navigate to Menu Management**
   - Management → Menu Management → Categories → Items

2. **Edit Menu Item**
   - Scroll to "Print Settings"
   - Click "Select Print Group" button

3. **Select Group**
   - Dialog shows all active print groups
   - Groups with assigned printers are highlighted
   - Select desired group
   - Cannot create new groups here (enforced)

4. **Save Item**
   - Item saved with `print_group_id`
   - When ordered, will route to assigned printer

#### Component Labels Workflow

1. **Enable Component Labels**
   - Toggle "Print Component Labels" on
   - ComponentLabelDialog appears

2. **Add Components**
   - Enter component name (e.g., "Roti", "Curry")
   - Press Enter or click "Add Component"
   - Repeat for all components

3. **View/Remove Components**
   - Components listed below toggle
   - Click "×" to remove component

4. **Save Item**
   - Components saved as JSON array
   - Labels printed separately when ordered

### Code Implementation

#### PrinterSetupPage.xaml.cs

**Key Methods**:
```csharp
private async void OnSelectPrintGroupClicked(object? sender, EventArgs e)
{
    // Get all print groups
    var allGroups = await _printGroupService.GetAllPrintGroupsAsync();
    
    // Show selector dialog
    var dialog = new PrintGroupSelectorDialog(allGroups, _selectedPrintGroupId);
    DialogOverlay.Content = dialog;
    DialogOverlay.IsVisible = true;
    
    var (selectedGroupId, action) = await dialog.ShowAsync();
    
    if (action == "create")
    {
        await ShowCreatePrintGroupDialogAsync();
    }
    else if (action == "select")
    {
        _selectedPrintGroupId = selectedGroupId;
        UpdatePrintGroupButton();
    }
}

private async Task ShowCreatePrintGroupDialogAsync()
{
    var createDialog = new CreatePrintGroupDialog();
    var (groupName, colorCode) = await createDialog.ShowAsync();
    
    if (!string.IsNullOrWhiteSpace(groupName))
    {
        var newGroup = new PrintGroup
        {
            Id = Guid.NewGuid().ToString(),
            Name = groupName,
            ColorCode = colorCode,
            IsActive = true
        };
        
        await _printGroupService.CreatePrintGroupAsync(newGroup);
        _selectedPrintGroupId = newGroup.Id;
        UpdatePrintGroupButton();
    }
}

private async void OnSavePrinterClicked(object? sender, EventArgs e)
{
    var printer = _editingPrinter ?? new NetworkPrinter();
    // ... set other properties ...
    printer.PrintGroupId = _selectedPrintGroupId;  // Save print group
    
    if (_editingPrinter != null)
        await _dbService.UpdatePrinterAsync(printer);
    else
        await _dbService.AddPrinterAsync(printer);
}
```

#### AddEditItemPage.xaml.cs

**Print Group Selection**:
```csharp
private async void OnSelectPrintGroupClicked(object? sender, EventArgs e)
{
    var allGroups = await _printGroupService!.GetAllPrintGroupsAsync();
    var activeGroups = allGroups.Where(g => g.IsActive).ToList();
    
    var dialog = new PrintGroupDialog(activeGroups, _selectedPrintGroupId);
    DialogOverlay.Content = dialog;
    DialogOverlay.IsVisible = true;
    
    var selectedId = await dialog.ShowAsync();
    
    if (!string.IsNullOrEmpty(selectedId))
    {
        _selectedPrintGroupId = selectedId;
        UpdatePrintGroupButton();
    }
}
```

**Component Labels**:
```csharp
private async void OnAddComponentLabelClicked(object? sender, EventArgs e)
{
    var dialog = new ComponentLabelDialog();
    DialogOverlay.Content = dialog;
    DialogOverlay.IsVisible = true;
    
    var componentName = await dialog.ShowAsync();
    
    if (!string.IsNullOrWhiteSpace(componentName))
    {
        _componentLabels.Add(componentName);
        LoadComponentLabels();
    }
}

private string GetComponentLabelsJson()
{
    return JsonSerializer.Serialize(_componentLabels);
}
```

### Database Migrations

#### printer_group_assignment_migration.sql
```sql
ALTER TABLE network_printers 
ADD COLUMN IF NOT EXISTS print_group_id VARCHAR(36) NULL;

CREATE INDEX IF NOT EXISTS idx_print_group_id 
ON network_printers(print_group_id);
```

### Testing Checklist

- [ ] Create new print group in Printer Setup
- [ ] Assign printer to existing print group
- [ ] Change printer's print group assignment
- [ ] Unassign printer from print group (select "None")
- [ ] Create menu item and select print group
- [ ] Enable component labels on menu item
- [ ] Add multiple component labels
- [ ] Remove component labels
- [ ] Save menu item with both print group and component labels
- [ ] Verify database stores both fields correctly
- [ ] Test that menu items can only select, not create groups

### Future Enhancements

1. **Order Routing Logic**
   - Group order items by `print_group_id`
   - Send each group to assigned printer
   - Fall back to default if no printer assigned

2. **Component Label Printing**
   - Generate labels for each component
   - Send to label printer (Brother QL-820NWB)
   - Include item name, component name, table number

3. **Print Group Analytics**
   - Track prints per group
   - Monitor printer health by group
   - Generate group-based reports

4. **Multi-Printer Support**
   - Allow multiple printers per group (load balancing)
   - Failover to backup printer if primary offline

5. **Print Group Templates**
   - Save common group configurations
   - Quick setup for new installations

## File Structure

```
POS-in-NET/
├── Models/
│   ├── NetworkPrinter.cs (updated with PrintGroupId)
│   └── FoodMenu/
│       ├── FoodMenuItem.cs (component_labels_json, print_group_id)
│       └── PrintGroup.cs (new)
├── Services/
│   ├── NetworkPrinterDatabaseService.cs (updated)
│   ├── MenuItemService.cs (updated)
│   └── PrintGroupService.cs (new)
├── Pages/
│   ├── PrinterSetupPage.xaml (updated with print group field)
│   ├── PrinterSetupPage.xaml.cs (print group selection logic)
│   ├── AddEditItemPage.xaml (print group button)
│   └── AddEditItemPage.xaml.cs (component labels + print group)
├── Views/
│   ├── PrintGroupSelectorDialog.xaml (new)
│   ├── PrintGroupSelectorDialog.xaml.cs (new)
│   ├── CreatePrintGroupDialog.xaml (new)
│   ├── CreatePrintGroupDialog.xaml.cs (new)
│   ├── ComponentLabelDialog.xaml (existing)
│   └── PrintGroupDialog.xaml (existing)
└── Database/
    └── printer_group_assignment_migration.sql (new)
```

## Summary

The Print Group system provides:
- ✅ Centralized printer group management in Printer Setup
- ✅ Easy printer-to-group assignment
- ✅ Menu item routing through print groups
- ✅ Component label support for detailed printing
- ✅ Clean separation of concerns (create vs. select)
- ✅ User-friendly dialogs with modern UI
- ✅ Database schema supporting full functionality

**Next Step**: Implement order routing logic to send grouped items to their assigned printers.
