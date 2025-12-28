using System.Collections.ObjectModel;
using MyFirstMauiApp.Models;
using MyFirstMauiApp.Models.FoodMenu;
using MyFirstMauiApp.Services;
using MySqlConnector;
using POS_in_NET.Services;
using Microsoft.Maui.Controls.Shapes;

namespace POS_in_NET.Pages;

public partial class AddEditItemPage : ContentPage
{
    private readonly MenuItemService _menuItemService;
    private readonly MenuCategoryService _categoryService;
    private readonly CommentNoteService _noteService;
    private readonly PrintGroupService _printGroupService;
    
    private ObservableCollection<MenuCategory> _allCategories = new();
    private List<MenuCategory> _topLevelCategories = new();
    private List<MenuCategory> _currentSubCategories = new();
    private ObservableCollection<Addon> _addons = new();
    private ObservableCollection<MenuItemComponent> _components = new();
    private ObservableCollection<MenuItemQuickNote> _quickNotes = new();
    private ObservableCollection<string> _componentLabels = new();
    private List<PrintGroup> _printGroups = new();
    private string _selectedColor = "#3B82F6";
    private string? _selectedCategoryId;
    private string? _selectedSubCategoryId;
    private FoodMenuItem? _editingItem;
    private bool _isEditMode = false;

    // Constructor for Add Mode
    public AddEditItemPage()
    {
        InitializeComponent();
        
        _menuItemService = new MenuItemService();
        _categoryService = new MenuCategoryService();
        _noteService = new CommentNoteService();
        _printGroupService = new PrintGroupService();
        
        // Use proper async initialization with error handling
        Dispatcher.Dispatch(async () =>
        {
            try
            {
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FATAL] Page initialization failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[FATAL] Stack trace: {ex.StackTrace}");
                await DisplayAlert("Initialization Error", $"Failed to load page: {ex.Message}", "OK");
                await Navigation.PopAsync();
            }
        });
    }

    // Constructor for Edit Mode
    public AddEditItemPage(FoodMenuItem item) : this()
    {
        _isEditMode = true;
        _editingItem = item;
        
        PageTitle.Text = "Edit Item";
        SaveButton.Text = "Update Item";
        
        LoadItemDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            // Load all data in parallel for speed
            var categoriesTask = _categoryService.GetAllCategoriesAsync();
            var notesTask = _noteService.GetAllNotesAsync();
            var printGroupsTask = _printGroupService.GetActivePrintGroupsAsync();
            
            await Task.WhenAll(categoriesTask, notesTask, printGroupsTask);
            
            // Cache all categories
            var allCategories = await categoriesTask;
            _allCategories.Clear();
            foreach (var cat in allCategories)
            {
                _allCategories.Add(cat);
            }
            
            // Cache print groups
            _printGroups = (await printGroupsTask).OrderBy(g => g.DisplayOrder).ToList();
            
            // Get only top-level categories (no parent) and cache them
            _topLevelCategories = allCategories
                .Where(c => string.IsNullOrEmpty(c.ParentId) || c.ParentId == "NULL")
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToList();
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Loaded {_topLevelCategories.Count} top-level categories");
            foreach (var cat in _topLevelCategories)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Top Category: {cat.Name} (ID: {cat.Id})");
            }
            
            // Set category picker source with category names
            if (_topLevelCategories.Any())
            {
                CategoryComboBox.ItemsSource = _topLevelCategories.Select(c => c.Name).ToList();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Category picker loaded with {_topLevelCategories.Count} items");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] WARNING: No top-level categories found!");
            }
            
            // Load VAT categories
            var vatCategories = VATCalculator.GetVatCategories();
            VatCategoryComboBox.ItemsSource = vatCategories.Select(v => v.Label).ToList();
            VatCategoryComboBox.SelectedIndex = 1; // Default to "Hot Food"
            
            // Load Print Groups
            LoadPrintGroups();
            
            // Initialize Quick Notes UI
            UpdateQuickNotesUI();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] LoadDataAsync failed: {ex.Message}");
            await DisplayAlert("Error", $"Failed to load data: {ex.Message}", "OK");
        }
    }

    private async void LoadItemDataAsync()
    {
        if (_editingItem == null) return;

        try
        {
            // Wait for initial data load (already started in constructor)
            while (_allCategories.Count == 0)
            {
                await Task.Delay(50); // Wait for data to load
            }
            
            // Basic info
            ItemNameEntry.Text = _editingItem.Name;
            PriceEntry.Text = _editingItem.Price.ToString("F2");
            _selectedColor = _editingItem.Color ?? "#3B82F6";
            CustomColorEntry.Text = _selectedColor;
            
            // Set category - find by ID first
            var category = _allCategories.FirstOrDefault(c => c.Id == _editingItem.CategoryId);
            if (category != null)
            {
                // Check if this is a sub-category (has parent)
                if (!string.IsNullOrEmpty(category.ParentId))
                {
                    // This is a sub-category, find parent first
                    var parentCategory = _allCategories.FirstOrDefault(c => c.Id == category.ParentId);
                    if (parentCategory != null)
                    {
                        CategoryComboBox.SelectedItem = parentCategory.Name;
                        _selectedCategoryId = parentCategory.Id;
                        
                        // Wait a moment for sub-categories to load
                        await Task.Delay(100);
                        
                        SubCategoryComboBox.SelectedItem = category.Name;
                        _selectedSubCategoryId = category.Id;
                    }
                }
                else
                {
                    // This is a top-level category
                    CategoryComboBox.SelectedItem = category.Name;
                    _selectedCategoryId = category.Id;
                }
            }
            
            // Load addons quickly
            if (_editingItem.Addons != null && _editingItem.Addons.Count > 0)
            {
                foreach (var addon in _editingItem.Addons)
                {
                    AddAddonCard(addon.Name, addon.Price);
                }
                UpdateAddonsEmptyState();
            }
            
            // Load VAT configuration
            if (!string.IsNullOrEmpty(_editingItem.VatConfigType))
            {
                if (_editingItem.VatConfigType == "component")
                {
                    MealDealRadio.IsChecked = true;
                    
                    // Load components
                    var components = await _menuItemService.GetItemComponentsAsync(_editingItem.Id);
                    _components.Clear();
                    foreach (var component in components)
                    {
                        _components.Add(component);
                        AddComponentCard(component);
                    }
                    UpdateVatBreakdown();
                }
                else
                {
                    StandardItemRadio.IsChecked = true;
                    
                    // Load VAT category
                    var vatCategories = VATCalculator.GetVatCategories();
                    var vatCategory = vatCategories.FirstOrDefault(v => v.Value == _editingItem.VatCategory);
                    if (vatCategory != null)
                    {
                        VatCategoryComboBox.SelectedItem = vatCategory.Label;
                    }
                }
            }
            
            // Load quick notes
            var quickNotes = await _menuItemService.GetQuickNotesAsync(_editingItem.Id);
            _quickNotes.Clear();
            foreach (var note in quickNotes)
            {
                _quickNotes.Add(note);
            }
            UpdateQuickNotesUI();
            
            // Load label print settings
            LabelTextEntry.Text = _editingItem.LabelText;
            PrintComponentLabelsSwitch.IsToggled = _editingItem.PrintComponentLabels;
            LoadComponentLabels(_editingItem.ComponentLabelsJson);
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Loaded print settings - LabelText: {_editingItem.LabelText}, PrintComponentLabels: {_editingItem.PrintComponentLabels}");
            
            // Load print group
            LoadPrintGroupSelection(_editingItem.PrintGroupId);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load item data: {ex.Message}", "OK");
        }
    }

    private void OnItemNameChanged(object sender, TextChangedEventArgs e)
    {
        // Character counter removed for cleaner UI
    }

    private void OnCategoryChanged(object sender, Syncfusion.Maui.Inputs.SelectionChangedEventArgs e)
    {
        try
        {
            var selectedCategoryName = e.AddedItems?.FirstOrDefault()?.ToString();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Category changed to: {selectedCategoryName}");
            
            if (string.IsNullOrEmpty(selectedCategoryName))
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Category name is empty, returning");
                return;
            }

            // Find selected category from cached top-level categories
            var selectedCategory = _topLevelCategories.FirstOrDefault(c => c.Name == selectedCategoryName);
            
            if (selectedCategory == null)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] WARNING: Could not find category '{selectedCategoryName}' in top-level list");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Found category: {selectedCategory.Name} (ID: {selectedCategory.Id})");
            _selectedCategoryId = selectedCategory.Id;
            
            // Auto-assign category color to item
            _selectedColor = selectedCategory.Color ?? "#3B82F6";
            CustomColorEntry.Text = _selectedColor;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Assigned color: {_selectedColor}");
            
            // Find sub-categories for this category
            _currentSubCategories = _allCategories
                .Where(c => c.ParentId == selectedCategory.Id)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToList();
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Found {_currentSubCategories.Count} sub-categories");
            
            if (_currentSubCategories.Count > 0)
            {
                foreach (var sub in _currentSubCategories)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG]   Sub-category: {sub.Name} (ID: {sub.Id})");
                }
                
                SubCategorySection.IsVisible = true;
                SubCategoryComboBox.ItemsSource = _currentSubCategories.Select(c => c.Name).ToList();
                SubCategoryComboBox.SelectedIndex = -1; // Reset selection
                _selectedSubCategoryId = null;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] No sub-categories, hiding section");
                SubCategorySection.IsVisible = false;
                SubCategoryComboBox.ItemsSource = null;
                SubCategoryComboBox.SelectedIndex = -1;
                _selectedSubCategoryId = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] OnCategoryChanged failed: {ex.Message}");
        }
    }

    private void OnSubCategoryChanged(object sender, Syncfusion.Maui.Inputs.SelectionChangedEventArgs e)
    {
        try
        {
            var selectedSubCategoryName = e.AddedItems?.FirstOrDefault() as string;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Sub-category changed to: {selectedSubCategoryName}");
            
            if (string.IsNullOrEmpty(selectedSubCategoryName))
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Sub-category name is empty");
                _selectedSubCategoryId = null;
                return;
            }

            // Find selected sub-category from current sub-categories list
            var selectedSubCategory = _currentSubCategories.FirstOrDefault(c => c.Name == selectedSubCategoryName);
            
            if (selectedSubCategory != null)
            {
                _selectedSubCategoryId = selectedSubCategory.Id;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Selected sub-category ID: {_selectedSubCategoryId}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] WARNING: Could not find sub-category '{selectedSubCategoryName}'");
                _selectedSubCategoryId = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] OnSubCategoryChanged failed: {ex.Message}");
        }
    }

    private void OnColorChipTapped(object sender, EventArgs e)
    {
        if (sender is Border chip)
        {
            // Color mapping for chips
            var colorMap = new Dictionary<string, string>
            {
                { "ColorChip1", "#EF4444" },  // Red
                { "ColorChip2", "#F59E0B" },  // Orange
                { "ColorChip3", "#10B981" },  // Green
                { "ColorChip4", "#3B82F6" },  // Blue
                { "ColorChip5", "#8B5CF6" },  // Purple
                { "ColorChip6", "#EC4899" }   // Pink
            };

            if (colorMap.TryGetValue(chip.StyleId ?? "", out var colorHex))
            {
                _selectedColor = colorHex;
                CustomColorEntry.Text = colorHex;
            }
            
            // Reset all chips
            ColorChip1.StrokeThickness = 2;
            ColorChip1.Stroke = Color.FromArgb("#FEE2E2");
            ColorChip2.StrokeThickness = 2;
            ColorChip2.Stroke = Color.FromArgb("#FED7AA");
            ColorChip3.StrokeThickness = 2;
            ColorChip3.Stroke = Color.FromArgb("#D1FAE5");
            ColorChip4.StrokeThickness = 2;
            ColorChip4.Stroke = Color.FromArgb("#DBEAFE");
            ColorChip5.StrokeThickness = 2;
            ColorChip5.Stroke = Color.FromArgb("#EDE9FE");
            ColorChip6.StrokeThickness = 2;
            ColorChip6.Stroke = Color.FromArgb("#FCE7F3");
            
            // Highlight selected
            chip.StrokeThickness = 3;
            chip.Stroke = Color.FromArgb(_selectedColor);
        }
    }

    private void OnCustomColorChanged(object sender, TextChangedEventArgs e)
    {
        var colorText = e.NewTextValue?.Trim();
        if (string.IsNullOrEmpty(colorText)) return;
        
        // Ensure # prefix
        if (!colorText.StartsWith("#"))
        {
            colorText = "#" + colorText;
        }
        
        // Validate hex color
        if (colorText.Length == 7 && colorText.All(c => "0123456789ABCDEFabcdef#".Contains(c)))
        {
            _selectedColor = colorText.ToUpper();
            
            // Reset all color chips
            ColorChip1.StrokeThickness = 2;
            ColorChip1.Stroke = Color.FromArgb("#FEE2E2");
            ColorChip2.StrokeThickness = 2;
            ColorChip2.Stroke = Color.FromArgb("#FED7AA");
            ColorChip3.StrokeThickness = 2;
            ColorChip3.Stroke = Color.FromArgb("#D1FAE5");
            ColorChip4.StrokeThickness = 2;
            ColorChip4.Stroke = Color.FromArgb("#DBEAFE");
            ColorChip5.StrokeThickness = 2;
            ColorChip5.Stroke = Color.FromArgb("#EDE9FE");
            ColorChip6.StrokeThickness = 2;
            ColorChip6.Stroke = Color.FromArgb("#FCE7F3");
        }
    }

    private async void OnAddAddonClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await AddAddonPopup.ShowAsync(this);
            
            if (result != null && result.WasSaved && !string.IsNullOrEmpty(result.AddonName))
            {
                var addon = new Addon
                {
                    Name = result.AddonName,
                    Price = result.AddonPrice
                };
                
                _addons.Add(addon);
                AddAddonCard(addon.Name, addon.Price);
                UpdateAddonsEmptyState();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to add addon: {ex.Message}", "OK");
        }
    }

    private void AddAddonCard(string name, decimal price)
    {
        var card = new Frame
        {
            BackgroundColor = Color.FromArgb("#F8FAFC"),
            BorderColor = Color.FromArgb("#E2E8F0"),
            CornerRadius = 8,
            Padding = 12,
            HasShadow = false,
            Margin = new Thickness(0, 4)
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        var nameLabel = new Label
        {
            Text = name,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1E293B"),
            VerticalOptions = LayoutOptions.Center
        };

        var priceLabel = new Label
        {
            Text = $"£{price:F2}",
            FontSize = 16,
            TextColor = Color.FromArgb("#64748B"),
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 12, 0)
        };

        var deleteButton = new Button
        {
            Text = "✕",
            FontSize = 18,
            BackgroundColor = Color.FromArgb("#EF4444"),
            TextColor = Colors.White,
            WidthRequest = 36,
            HeightRequest = 36,
            CornerRadius = 18,
            Padding = 0
        };

        var addon = _addons.FirstOrDefault(a => a.Name == name && a.Price == price);
        deleteButton.Clicked += async (s, e) =>
        {
            if (addon != null)
            {
                _addons.Remove(addon);
                AddonsContainer.Children.Remove(card);
                UpdateAddonsEmptyState();
            }
        };

        var infoStack = new HorizontalStackLayout
        {
            Spacing = 8,
            Children = { nameLabel, priceLabel }
        };

        grid.Children.Add(infoStack);
        grid.Children.Add(deleteButton);
        Grid.SetColumn(deleteButton, 1);

        card.Content = grid;
        AddonsContainer.Children.Add(card);
    }

    private void UpdateAddonsEmptyState()
    {
        AddonsEmptyState.IsVisible = !_addons.Any();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(ItemNameEntry.Text))
            {
                await DisplayAlert("Validation Error", "Please enter an item name.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(_selectedCategoryId))
            {
                await DisplayAlert("Validation Error", "Please select a category.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(PriceEntry.Text) || !decimal.TryParse(PriceEntry.Text, out decimal price) || price <= 0)
            {
                await DisplayAlert("Validation Error", "Please enter a valid price.", "OK");
                return;
            }

            // Determine final category ID to save
            // If sub-category is selected, use it; otherwise use main category
            string finalCategoryId = _selectedSubCategoryId ?? _selectedCategoryId;
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Saving item:");
            System.Diagnostics.Debug.WriteLine($"[DEBUG]   Name: {ItemNameEntry.Text}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG]   Category ID: {_selectedCategoryId}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG]   Sub-Category ID: {_selectedSubCategoryId}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG]   Final Category ID: {finalCategoryId}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG]   Price: {price}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG]   Color: {_selectedColor}");

            if (_isEditMode && _editingItem != null)
            {
                // Update existing item
                _editingItem.Name = ItemNameEntry.Text.Trim();
                _editingItem.CategoryId = finalCategoryId;
                _editingItem.Price = price;
                _editingItem.Color = _selectedColor;
                _editingItem.Addons = _addons.ToList();
                _editingItem.UpdatedAt = DateTime.Now;
                
                // Update VAT configuration
                bool isStandard = StandardItemRadio.IsChecked;
                _editingItem.VatConfigType = isStandard ? "standard" : "component";
                
                if (isStandard)
                {
                    // Get selected VAT category
                    var selectedLabel = VatCategoryComboBox.SelectedItem?.ToString();
                    var vatCategories = VATCalculator.GetVatCategories();
                    var selectedCategory = vatCategories.FirstOrDefault(v => v.Label == selectedLabel);
                    _editingItem.VatCategory = selectedCategory?.Value ?? "HotFood";
                    _editingItem.CalculatedVatRate = _editingItem.VatCategory == "NoVAT" ? 0 : 20;
                }
                else
                {
                    // Calculate effective VAT rate from components
                    decimal totalPrice = _components.Sum(c => c.ComponentPrice);
                    decimal totalVat = _components.Sum(c => c.ComponentType switch
                    {
                        "ColdFood" => 0,
                        "ColdBeverage" => 0,
                        _ => c.ComponentPrice * 0.20m
                    });
                    _editingItem.CalculatedVatRate = totalPrice > 0 ? (totalVat / totalPrice) * 100 : 0;
                }
                
                // Keep legacy fields for backward compatibility
                _editingItem.VatRate = _editingItem.CalculatedVatRate;
                _editingItem.VatType = _editingItem.VatConfigType;
                
                // Save label print settings
                _editingItem.LabelText = string.IsNullOrWhiteSpace(LabelTextEntry.Text) ? null : LabelTextEntry.Text.Trim();
                _editingItem.PrintComponentLabels = PrintComponentLabelsSwitch.IsToggled;
                _editingItem.ComponentLabelsJson = GetComponentLabelsJson();
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Print settings - LabelText: {_editingItem.LabelText}, PrintComponentLabels: {_editingItem.PrintComponentLabels}, ComponentLabels: {_editingItem.ComponentLabelsJson}");
                
                // Save print group
                _editingItem.PrintGroupId = GetSelectedPrintGroupId();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Updating item with CategoryId: {_editingItem.CategoryId}");
                await _menuItemService.UpdateItemAsync(_editingItem);
                
                // Save components if meal deal
                if (!isStandard)
                {
                    await _menuItemService.SaveItemComponentsAsync(_editingItem.Id, _components.ToList());
                }
                
                // Save quick notes
                await _menuItemService.SaveQuickNotesAsync(_editingItem.Id, _quickNotes.ToList());

                await DisplayAlert("Success", "Item updated successfully!", "OK");
            }
            else
            {
                // Create new item
                bool isStandard = StandardItemRadio.IsChecked;
                
                // Get VAT category and calculate rate
                string vatCategory = "HotFood";
                decimal calculatedVatRate = 20;
                
                if (isStandard)
                {
                    var selectedLabel = VatCategoryComboBox.SelectedItem?.ToString();
                    var vatCategories = VATCalculator.GetVatCategories();
                    var selectedCategory = vatCategories.FirstOrDefault(v => v.Label == selectedLabel);
                    vatCategory = selectedCategory?.Value ?? "HotFood";
                    calculatedVatRate = vatCategory == "NoVAT" ? 0 : 20;
                }
                else
                {
                    // Calculate from components
                    decimal totalPrice = _components.Sum(c => c.ComponentPrice);
                    decimal totalVat = _components.Sum(c => c.ComponentType switch
                    {
                        "ColdFood" => 0,
                        "ColdBeverage" => 0,
                        _ => c.ComponentPrice * 0.20m
                    });
                    calculatedVatRate = totalPrice > 0 ? (totalVat / totalPrice) * 100 : 0;
                }
                
                var newItem = new FoodMenuItem
                {
                    Id = Guid.NewGuid().ToString(),
                    CategoryId = finalCategoryId,
                    Name = ItemNameEntry.Text.Trim(),
                    Price = price,
                    Color = _selectedColor,
                    Addons = _addons.ToList(),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    
                    // New VAT fields
                    VatConfigType = isStandard ? "standard" : "component",
                    VatCategory = vatCategory,
                    CalculatedVatRate = calculatedVatRate,
                    
                    // Legacy fields
                    VatRate = calculatedVatRate,
                    VatType = isStandard ? "simple" : "component",
                    IsVatExempt = vatCategory == "NoVAT",
                    
                    // Label print settings
                    LabelText = string.IsNullOrWhiteSpace(LabelTextEntry.Text) ? null : LabelTextEntry.Text.Trim(),
                    PrintComponentLabels = PrintComponentLabelsSwitch.IsToggled,
                    ComponentLabelsJson = GetComponentLabelsJson(),
                    
                    // Print group
                    PrintGroupId = GetSelectedPrintGroupId()
                };

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Creating new item with CategoryId: {newItem.CategoryId}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] VAT Config: Type={newItem.VatConfigType}, Category={newItem.VatCategory}, Rate={newItem.CalculatedVatRate}");
                
                var createResult = await _menuItemService.CreateItemAsync(newItem);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Create result: {createResult}");
                
                // Save components if meal deal
                if (!isStandard && _components.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Saving {_components.Count} components...");
                    await _menuItemService.SaveItemComponentsAsync(newItem.Id, _components.ToList());
                }
                
                // Save quick notes
                if (_quickNotes.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Saving {_quickNotes.Count} quick notes...");
                    await _menuItemService.SaveQuickNotesAsync(newItem.Id, _quickNotes.ToList());
                }

                await DisplayAlert("Success", "Item created successfully!", "OK");
            }

            // Go back
            await Navigation.PopAsync();
        }
        catch (MySqlConnector.MySqlException mysqlEx)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] MySQL Error: {mysqlEx.Message}");
            System.Diagnostics.Debug.WriteLine($"[ERROR] MySQL Error Number: {mysqlEx.Number}");
            System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {mysqlEx.StackTrace}");
            await DisplayAlert("Database Error", $"Failed to save item: {mysqlEx.Message}\n\nError Code: {mysqlEx.Number}", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] General Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            await DisplayAlert("Error", $"Failed to save item: {ex.Message}\n\n{ex.GetType().Name}", "OK");
        }
    }

    // ========================================
    // VAT Configuration Handlers
    // ========================================

    private void OnVatTypeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is RadioButton radio && radio.IsChecked)
        {
            bool isStandard = radio == StandardItemRadio;
            StandardVatSection.IsVisible = isStandard;
            ComponentVatSection.IsVisible = !isStandard;
            
            if (!isStandard)
            {
                // Show example component for meal deals
                if (_components.Count == 0)
                {
                    AddExampleComponents();
                }
            }
        }
    }

    private void OnVatCategoryChanged(object sender, Syncfusion.Maui.Inputs.SelectionChangedEventArgs e)
    {
        var selectedLabel = e.AddedItems?.FirstOrDefault()?.ToString();
        if (string.IsNullOrEmpty(selectedLabel)) return;

        var vatCategories = VATCalculator.GetVatCategories();
        var selected = vatCategories.FirstOrDefault(v => v.Label == selectedLabel);
        
        if (selected != null)
        {
            // Update info label
            VatInfoLabel.Text = $"💡 {selected.Description}";
        }
    }

    private void OnAddComponentClicked(object sender, EventArgs e)
    {
        AddComponentCard();
    }

    private void AddExampleComponents()
    {
        // Add example components for first-time users
        var component1 = new MenuItemComponent
        {
            ComponentName = "Main Item (Hot)",
            ComponentPrice = 12.00m,
            ComponentType = "HotFood",
            SortOrder = 1
        };
        
        var component2 = new MenuItemComponent
        {
            ComponentName = "Side (Cold)",
            ComponentPrice = 3.00m,
            ComponentType = "ColdFood",
            SortOrder = 2
        };

        _components.Add(component1);
        _components.Add(component2);
        
        AddComponentCard(component1);
        AddComponentCard(component2);
        
        UpdateVatBreakdown();
    }

    private void AddComponentCard(MenuItemComponent? component = null)
    {
        var newComponent = component ?? new MenuItemComponent
        {
            ComponentName = "",
            ComponentPrice = 0m,
            ComponentType = "HotFood",
            SortOrder = _components.Count + 1
        };

        if (component == null)
        {
            _components.Add(newComponent);
        }

        // Create component card UI
        var card = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 1,
            Stroke = Color.FromArgb("#E2E8F0"),
            Padding = new Thickness(16, 12),
            Margin = new Thickness(0, 0, 0, 8)
        };
        card.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 };

        var stack = new VerticalStackLayout { Spacing = 10 };

        // Component number and remove button
        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        var numberLabel = new Label
        {
            Text = $"Component {newComponent.SortOrder}",
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#475569")
        };

        var removeButton = new Button
        {
            Text = "Remove",
            BackgroundColor = Color.FromArgb("#EF4444"),
            TextColor = Colors.White,
            FontSize = 12,
            CornerRadius = 6,
            HeightRequest = 32,
            Padding = new Thickness(12, 0)
        };
        removeButton.Clicked += (s, e) =>
        {
            _components.Remove(newComponent);
            ComponentsContainer.Children.Remove(card);
            UpdateVatBreakdown();
        };

        headerGrid.Add(numberLabel, 0, 0);
        headerGrid.Add(removeButton, 1, 0);
        stack.Add(headerGrid);

        // Component name
        var nameEntry = new Entry
        {
            Placeholder = "e.g., Chicken Biryani",
            Text = newComponent.ComponentName,
            FontSize = 14
        };
        nameEntry.TextChanged += (s, e) =>
        {
            newComponent.ComponentName = e.NewTextValue;
            UpdateVatBreakdown();
        };
        stack.Add(nameEntry);

        // Price and Type grid
        var detailsGrid = new Grid
        {
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
        };

        // Price entry
        var priceStack = new VerticalStackLayout { Spacing = 4 };
        priceStack.Add(new Label { Text = "Price (£)", FontSize = 12, TextColor = Color.FromArgb("#64748B") });
        var priceEntry = new Entry
        {
            Placeholder = "0.00",
            Keyboard = Keyboard.Numeric,
            Text = newComponent.ComponentPrice > 0 ? newComponent.ComponentPrice.ToString("F2") : "",
            FontSize = 14
        };
        priceEntry.TextChanged += (s, e) =>
        {
            if (decimal.TryParse(e.NewTextValue, out var price))
            {
                newComponent.ComponentPrice = price;
                UpdateVatBreakdown();
            }
        };
        priceStack.Add(priceEntry);
        detailsGrid.Add(priceStack, 0, 0);

        // Type selector with modern card-based UI
        var typeStack = new VerticalStackLayout { Spacing = 8 };
        typeStack.Add(new Label 
        { 
            Text = "Component Type", 
            FontSize = 13, 
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1E293B") 
        });

        var typeGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            },
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            },
            RowSpacing = 8,
            ColumnSpacing = 8
        };

        var componentTypes = new[]
        {
            new { Display = "Hot Food", Value = "HotFood", Color = "#EF4444" },
            new { Display = "Cold Food", Value = "ColdFood", Color = "#3B82F6" },
            new { Display = "Hot Beverage", Value = "HotBeverage", Color = "#F59E0B" },
            new { Display = "Cold Beverage", Value = "ColdBeverage", Color = "#06B6D4" },
            new { Display = "Alcohol", Value = "Alcohol", Color = "#8B5CF6" }
        };

        var typeButtons = new List<Border>();
        int row = 0, col = 0;

        foreach (var type in componentTypes)
        {
            var isSelected = newComponent.ComponentType == type.Value;
            
            var border = new Border
            {
                BackgroundColor = isSelected ? Color.FromArgb(type.Color) : Colors.White,
                Stroke = Color.FromArgb(type.Color),
                StrokeThickness = 2,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(12, 10),
                HeightRequest = 50
            };

            var label = new Label
            {
                Text = type.Display,
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = isSelected ? Colors.White : Color.FromArgb(type.Color),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            border.Content = label;
            
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) =>
            {
                // Reset all buttons
                foreach (var btn in typeButtons)
                {
                    var btnLabel = (Label)btn.Content;
                    var btnColor = btn.Stroke.ToString();
                    btn.BackgroundColor = Colors.White;
                    btnLabel.TextColor = Color.FromArgb(btnColor);
                }

                // Select this button
                border.BackgroundColor = Color.FromArgb(type.Color);
                label.TextColor = Colors.White;
                
                // Update component type
                newComponent.ComponentType = type.Value;
                UpdateVatBreakdown();
            };
            
            border.GestureRecognizers.Add(tapGesture);
            typeButtons.Add(border);

            typeGrid.Add(border, col, row);
            
            col++;
            if (col > 1)
            {
                col = 0;
                row++;
            }
        }

        typeStack.Add(typeGrid);
        detailsGrid.Add(typeStack, 1, 0);

        stack.Add(detailsGrid);
        card.Content = stack;
        ComponentsContainer.Add(card);
    }

    private void UpdateVatBreakdown()
    {
        if (_components.Count == 0)
        {
            VatBreakdownBorder.IsVisible = false;
            return;
        }

        VatBreakdownBorder.IsVisible = true;
        VatBreakdownStack.Clear();

        decimal totalPrice = 0m;
        decimal totalVat = 0m;

        foreach (var component in _components)
        {
            if (component.ComponentPrice <= 0) continue;

            totalPrice += component.ComponentPrice;
            decimal componentVat = component.ComponentType switch
            {
                "ColdFood" => 0m,
                "ColdBeverage" => 0m,
                _ => component.ComponentPrice * 0.20m
            };
            totalVat += componentVat;

            var row = new HorizontalStackLayout { Spacing = 8 };
            row.Add(new Label
            {
                Text = $"• {component.ComponentName}: £{component.ComponentPrice:F2} × ",
                FontSize = 12,
                TextColor = Color.FromArgb("#64748B")
            });
            row.Add(new Label
            {
                Text = componentVat > 0 ? "20%" : "0%",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = componentVat > 0 ? Color.FromArgb("#10B981") : Color.FromArgb("#64748B")
            });
            row.Add(new Label
            {
                Text = $" = £{componentVat:F2}",
                FontSize = 12,
                TextColor = Color.FromArgb("#64748B")
            });
            VatBreakdownStack.Add(row);
        }

        // Calculate effective rate
        decimal effectiveRate = totalPrice > 0 ? (totalVat / totalPrice) * 100 : 0;
        EffectiveVatLabel.Text = $"{effectiveRate:F0}%";
    }

    // ===================================
    // PRINT GROUP HANDLERS
    // ===================================
    
    private void LoadPrintGroups()
    {
        // Print groups are loaded on-demand when needed
    }
    
    private void LoadPrintGroupSelection(string? printGroupId)
    {
        _selectedPrintGroupId = printGroupId;
        UpdatePrintGroupButton();
    }
    
    private async void OnSelectPrintGroupClicked(object sender, EventArgs e)
    {
        if (_printGroupService == null) return;

        try
        {
            // Get all print groups created in Printer Setup
            var allGroups = await _printGroupService.GetAllPrintGroupsAsync();
            var activeGroups = allGroups.Where(g => g.IsActive).ToList();

            if (!activeGroups.Any())
            {
                await DisplayAlert("No Print Groups", "No print groups found. Please create print groups in Printer Setup first.", "OK");
                return;
            }

            // Show selection dialog
            var dialog = new Views.PrintGroupDialog(activeGroups, _selectedPrintGroupId);
            DialogOverlay.Content = dialog;
            DialogOverlay.IsVisible = true;

            var selectedId = await dialog.ShowAsync();

            DialogOverlay.IsVisible = false;
            DialogOverlay.Content = null;

            // Update selection
            if (!string.IsNullOrEmpty(selectedId))
            {
                _selectedPrintGroupId = selectedId;
                UpdatePrintGroupButton();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load print groups: {ex.Message}", "OK");
        }
    }

    private void UpdatePrintGroupButton()
    {
        if (string.IsNullOrEmpty(_selectedPrintGroupId))
        {
            PrintGroupButton.Text = "Select Print Group...";
            PrintGroupButton.TextColor = Color.FromArgb("#94A3B8");
            return;
        }

        // Find group name
        if (_printGroupService != null)
        {
            Task.Run(async () =>
            {
                try
                {
                    var allGroups = await _printGroupService.GetAllPrintGroupsAsync();
                    var group = allGroups.FirstOrDefault(g => g.Id == _selectedPrintGroupId);
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (group != null)
                        {
                            PrintGroupButton.Text = $"✓ {group.Name}";
                            PrintGroupButton.TextColor = Color.FromArgb("#22C55E");
                        }
                    });
                }
                catch { }
            });
        }
    }
    
    private string? GetSelectedPrintGroupId()
    {
        return _selectedPrintGroupId;
    }
    
    private Task<string?> GetSelectedPrintGroupIdAsync()
    {
        return Task.FromResult(_selectedPrintGroupId);
    }
    
    private string? _selectedPrintGroupId;

    // ===================================
    // QUICK NOTES HANDLERS
    // ===================================
    
    private void UpdateQuickNotesUI()
    {
        QuickNotesCollectionView.ItemsSource = _quickNotes;
        QuickNotesCountLabel.Text = $"{_quickNotes.Count}/6";
        AddQuickNoteButton.IsEnabled = _quickNotes.Count < 6;
        AddQuickNoteButton.Opacity = _quickNotes.Count < 6 ? 1.0 : 0.5;
        
        // Update display order
        for (int i = 0; i < _quickNotes.Count; i++)
        {
            _quickNotes[i].DisplayOrder = i + 1;
        }
    }

    private async void OnAddQuickNoteClicked(object sender, EventArgs e)
    {
        if (_quickNotes.Count >= 6)
        {
            await DisplayAlert("Maximum Reached", "You can only add up to 6 quick notes per item.", "OK");
            return;
        }

        var dialog = new Views.QuickNoteDialog();
        
        // Add dialog to page
        if (Content is Grid mainGrid)
        {
            Grid.SetRowSpan(dialog, mainGrid.RowDefinitions.Count > 0 ? mainGrid.RowDefinitions.Count : 1);
            Grid.SetColumnSpan(dialog, mainGrid.ColumnDefinitions.Count > 0 ? mainGrid.ColumnDefinitions.Count : 1);
            mainGrid.Children.Add(dialog);
            
            var result = await dialog.ShowAsync();
            
            mainGrid.Children.Remove(dialog);
            
            if (result != null)
            {
                result.DisplayOrder = _quickNotes.Count + 1;
                _quickNotes.Add(result);
                UpdateQuickNotesUI();
            }
        }
    }

    private async void OnEditQuickNoteClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is MenuItemQuickNote note)
        {
            var dialog = new Views.QuickNoteDialog();
            
            if (Content is Grid mainGrid)
            {
                Grid.SetRowSpan(dialog, mainGrid.RowDefinitions.Count > 0 ? mainGrid.RowDefinitions.Count : 1);
                Grid.SetColumnSpan(dialog, mainGrid.ColumnDefinitions.Count > 0 ? mainGrid.ColumnDefinitions.Count : 1);
                mainGrid.Children.Add(dialog);
                
                var result = await dialog.ShowAsync(note);
                
                mainGrid.Children.Remove(dialog);
                
                if (result != null)
                {
                    UpdateQuickNotesUI();
                }
            }
        }
    }

    private async void OnDeleteQuickNoteClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is MenuItemQuickNote note)
        {
            bool confirm = await DisplayAlert("Delete Quick Note", 
                $"Are you sure you want to delete '{note.NoteText}'?", 
                "Delete", "Cancel");
            
            if (confirm)
            {
                _quickNotes.Remove(note);
                UpdateQuickNotesUI();
            }
        }
    }

    // ===================================
    // COMPONENT LABELS HANDLERS
    // ===================================
    
    private void OnPrintComponentLabelsToggled(object sender, ToggledEventArgs e)
    {
        ComponentsListContainer.IsVisible = e.Value;
        
        if (e.Value && _componentLabels.Count == 0)
        {
            // Auto-suggest adding a component
            Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(100); // Small delay for UI to update
                OnAddComponentLabelClicked(sender, EventArgs.Empty);
            });
        }
    }
    
    private async void OnAddComponentLabelClicked(object sender, EventArgs e)
    {
        var dialog = new Views.ComponentLabelDialog();
        
        // Show dialog in the overlay container
        DialogOverlay.Content = dialog;
        DialogOverlay.IsVisible = true;
        
        var result = await dialog.ShowAsync();
        
        // Hide dialog
        DialogOverlay.IsVisible = false;
        DialogOverlay.Content = null;
        
        if (!string.IsNullOrWhiteSpace(result))
        {
            var componentName = result.Trim();
            if (!_componentLabels.Contains(componentName))
            {
                _componentLabels.Add(componentName);
                UpdateComponentLabelsUI();
            }
            else
            {
                await DisplayAlert("Duplicate", $"'{componentName}' already exists in the list.", "OK");
            }
        }
    }
    
    private async void OnRemoveComponentLabel(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string componentLabel)
        {
            bool confirm = await DisplayAlert("Remove Component Label", 
                $"Remove '{componentLabel}' from print labels?", 
                "Remove", "Cancel");
            
            if (confirm)
            {
                _componentLabels.Remove(componentLabel);
                UpdateComponentLabelsUI();
            }
        }
    }
    
    private void UpdateComponentLabelsUI()
    {
        ComponentLabelsCollectionView.ItemsSource = null;
        ComponentLabelsCollectionView.ItemsSource = _componentLabels;
    }
    
    private void LoadComponentLabels(string? componentLabelsJson)
    {
        _componentLabels.Clear();
        
        if (string.IsNullOrWhiteSpace(componentLabelsJson))
            return;
            
        try
        {
            var labels = System.Text.Json.JsonSerializer.Deserialize<List<string>>(componentLabelsJson);
            if (labels != null)
            {
                foreach (var label in labels)
                {
                    _componentLabels.Add(label);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to parse component labels: {ex.Message}");
        }
        
        UpdateComponentLabelsUI();
        ComponentsListContainer.IsVisible = _componentLabels.Count > 0;
    }
    
    private string? GetComponentLabelsJson()
    {
        if (_componentLabels.Count == 0)
            return null;
            
        return System.Text.Json.JsonSerializer.Serialize(_componentLabels.ToList());
    }
}

