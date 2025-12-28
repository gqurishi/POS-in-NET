using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using MyFirstMauiApp.Models.FoodMenu;
using MyFirstMauiApp.Services;
using POS_in_NET.Models;
using POS_in_NET.Views;
using POS_in_NET.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace POS_in_NET.Pages
{
    // Helper class for category pagination
    public class CategoryPage
    {
        public ObservableCollection<CategoryButtonModel> Row1 { get; set; } = new();
        public ObservableCollection<CategoryButtonModel> Row2 { get; set; } = new();
    }

    // Model for category buttons with color binding
    public class CategoryButtonModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Color Color { get; set; } = Colors.Blue;
        public MenuCategory Category { get; set; } = null!;
    }

    public partial class OrderPlacementPageSimple : ContentPage
    {
        private readonly MenuItemService _menuItemService;
        private readonly MenuCategoryService _categoryService;
        
        private List<MenuCategory> _allCategories = new();
        private List<FoodMenuItem> _allMenuItems = new();
        private TableOrder _currentOrder = null!;
        private System.Timers.Timer? _timer;
        private MenuCategory? _selectedCategory;
        private MenuCategory? _selectedSubCategory;
        private string _searchQuery = string.Empty;
        
        // Collection order fields
        private bool _isCollectionOrder = false;
        private int _collectionCustomerId = 0;
        private string _collectionCustomerName = string.Empty;
        private string _collectionCustomerPhone = string.Empty;

        // Delivery order fields
        private bool _isDeliveryOrder = false;
        private int _deliveryCustomerId = 0;
        private string _deliveryCustomerName = string.Empty;
        private string _deliveryCustomerPhone = string.Empty;
        private string _deliveryCustomerAddress = string.Empty;

        public OrderPlacementPageSimple()
        {
            InitializeComponent();
            
            _menuItemService = new MenuItemService();
            _categoryService = new MenuCategoryService();
            
            InitializeOrder(1, 2, "Current User", 1);
        }

        public OrderPlacementPageSimple(string tableNumber, int coverCount, string staffName = "Staff", int staffId = 1)
        {
            InitializeComponent();
            
            _menuItemService = new MenuItemService();
            _categoryService = new MenuCategoryService();
            
            int tableNum = int.TryParse(tableNumber, out int num) ? num : 1;
            InitializeOrder(tableNum, coverCount, staffName, staffId);
        }

        public void SetCollectionOrderInfo(int customerId, string customerName, string customerPhone)
        {
            _isCollectionOrder = true;
            _collectionCustomerId = customerId;
            _collectionCustomerName = customerName;
            _collectionCustomerPhone = customerPhone;
            
            // Update TopBar to show Collection Order
            if (TopBar != null)
            {
                TopBar.SetPageTitle($"Collection Order - {customerName}");
            }
        }

        public void SetDeliveryOrderInfo(int customerId, string customerName, string customerPhone, string address)
        {
            _isDeliveryOrder = true;
            _deliveryCustomerId = customerId;
            _deliveryCustomerName = customerName;
            _deliveryCustomerPhone = customerPhone;
            _deliveryCustomerAddress = address;
            
            // Update TopBar to show Delivery Order
            if (TopBar != null)
            {
                TopBar.SetPageTitle($"Delivery Order - {customerName}");
            }
        }

        private void InitializeOrder(int tableNumber, int coverCount, string staffName, int staffId)
        {
            _currentOrder = new TableOrder
            {
                Id = Guid.NewGuid().ToString(),
                TableNumber = tableNumber,
                CoverCount = coverCount,
                StartTime = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                StaffId = staffId,
                StaffName = staffName,
                ServiceChargePercent = 0
            };
            
            StartTimer();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Set TopBar title
            if (TopBar != null)
            {
                if (_isCollectionOrder)
                {
                    TopBar.SetPageTitle($"Collection Order - {_collectionCustomerName}");
                }
                else if (_isDeliveryOrder)
                {
                    TopBar.SetPageTitle($"Delivery Order - {_deliveryCustomerName}");
                }
                else
                {
                    TopBar.SetPageTitle($"Table {_currentOrder.TableNumber}");
                }
            }
            
            // Load data in background - don't block UI
            _ = LoadDataInBackgroundAsync();
        }
        
        private async Task LoadDataInBackgroundAsync()
        {
            try
            {
                await LoadDataAsync();
                UpdateDisplay();
            }
            catch (Exception ex)
            {
                var errorDialog = new ModernAlertDialog();
                errorDialog.SetAlert("Error", $"Failed to load: {ex.Message}", "❌", "#EF4444", "White");
                await errorDialog.ShowAsync();
                System.Diagnostics.Debug.WriteLine($"[OrderPlacement] ERROR: {ex}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            StopTimer();
        }

        private void StartTimer()
        {
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TimerLabel.Text = $"⏱ {_currentOrder.ElapsedTimeDisplay}";
                });
            };
            _timer.Start();
        }

        private void StopTimer()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }

        private async Task LoadDataAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[OrderPlacement] Loading data...");
                
                // Load categories and menu items in parallel for faster loading
                var categoriesTask = _categoryService.GetAllCategoriesAsync();
                var itemsTask = _menuItemService.GetAllItemsAsync();
                
                await Task.WhenAll(categoriesTask, itemsTask);
                
                _allCategories = await categoriesTask;
                _allMenuItems = await itemsTask;
                
                System.Diagnostics.Debug.WriteLine($"[OrderPlacement] Loaded {_allCategories.Count} categories and {_allMenuItems.Count} items");
                
                BuildCategories();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OrderPlacement] Load error: {ex}");
                throw;
            }
        }

        private void BuildCategories()
        {
            var topCategories = _allCategories
                .Where(c => c.ParentId == null && c.Active)
                .OrderBy(c => c.DisplayOrder)
                .ToList();
            
            System.Diagnostics.Debug.WriteLine($"[OrderPlacement] Building {topCategories.Count} category buttons");
            
            // Group categories into pages of 10 (2 rows × 5 columns)
            var categoryPages = new ObservableCollection<CategoryPage>();
            const int BUTTONS_PER_PAGE = 10;
            
            for (int i = 0; i < topCategories.Count; i += BUTTONS_PER_PAGE)
            {
                var page = new CategoryPage();
                var pageCategories = topCategories.Skip(i).Take(BUTTONS_PER_PAGE).ToList();
                
                // First 5 go to Row 1
                for (int j = 0; j < Math.Min(5, pageCategories.Count); j++)
                {
                    var cat = pageCategories[j];
                    page.Row1.Add(new CategoryButtonModel
                    {
                        Id = cat.Id,
                        Name = cat.Name,
                        Color = Color.FromArgb(cat.Color),
                        Category = cat
                    });
                }
                
                // Next 5 go to Row 2
                for (int j = 5; j < pageCategories.Count; j++)
                {
                    var cat = pageCategories[j];
                    page.Row2.Add(new CategoryButtonModel
                    {
                        Id = cat.Id,
                        Name = cat.Name,
                        Color = Color.FromArgb(cat.Color),
                        Category = cat
                    });
                }
                
                categoryPages.Add(page);
            }
            
            CategoryCarousel.ItemsSource = categoryPages;
            
            // Auto-load first category
            if (topCategories.Any())
            {
                SelectCategory(topCategories.First());
            }
        }

        private void OnCategoryButtonClicked(object? sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is CategoryButtonModel model)
            {
                SelectCategory(model.Category);
            }
        }

        private void SelectCategory(MenuCategory category)
        {
            System.Diagnostics.Debug.WriteLine($"[OrderPlacement] Category selected: {category.Name}");
            
            _selectedCategory = category;
            _selectedSubCategory = null;
            _searchQuery = string.Empty;
            
            // Clear search display
            UpdateSearchDisplay();
            
            // Check if this category has sub-categories
            var subCategories = _allCategories
                .Where(c => c.ParentId == category.Id && c.Active)
                .OrderBy(c => c.DisplayOrder)
                .ToList();
            
            if (subCategories.Any())
            {
                // Show sub-categories
                BuildSubCategories(subCategories);
                SubCategorySection.IsVisible = true;
                
                // Auto-select first sub-category
                SelectSubCategory(subCategories.First());
            }
            else
            {
                // No sub-categories, load items directly
                SubCategorySection.IsVisible = false;
                LoadItemsForCategory(category.Id);
            }
        }

        private void BuildSubCategories(List<MenuCategory> subCategories)
        {
            System.Diagnostics.Debug.WriteLine($"[OrderPlacement] Building {subCategories.Count} sub-category buttons");
            
            // Use lighter shade of parent category color
            var lightColor = LightenColor(_selectedCategory?.Color ?? "#3B82F6");
            var buttonColor = Color.FromArgb(lightColor);
            
            // Group sub-categories into pages of 10 (2 rows × 5 columns)
            var subCategoryPages = new ObservableCollection<CategoryPage>();
            const int BUTTONS_PER_PAGE = 10;
            
            for (int i = 0; i < subCategories.Count; i += BUTTONS_PER_PAGE)
            {
                var page = new CategoryPage();
                var pageSubCategories = subCategories.Skip(i).Take(BUTTONS_PER_PAGE).ToList();
                
                // First 5 go to Row 1
                for (int j = 0; j < Math.Min(5, pageSubCategories.Count); j++)
                {
                    var subCat = pageSubCategories[j];
                    page.Row1.Add(new CategoryButtonModel
                    {
                        Id = subCat.Id,
                        Name = subCat.Name,
                        Color = buttonColor,
                        Category = subCat
                    });
                }
                
                // Next 5 go to Row 2
                for (int j = 5; j < pageSubCategories.Count; j++)
                {
                    var subCat = pageSubCategories[j];
                    page.Row2.Add(new CategoryButtonModel
                    {
                        Id = subCat.Id,
                        Name = subCat.Name,
                        Color = buttonColor,
                        Category = subCat
                    });
                }
                
                subCategoryPages.Add(page);
            }
            
            SubCategoryCarousel.ItemsSource = subCategoryPages;
        }

        private void OnSubCategoryButtonClicked(object? sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is CategoryButtonModel model)
            {
                SelectSubCategory(model.Category);
            }
        }

        private void SelectSubCategory(MenuCategory subCategory)
        {
            System.Diagnostics.Debug.WriteLine($"[OrderPlacement] Sub-category selected: {subCategory.Name}");
            
            _selectedSubCategory = subCategory;
            LoadItemsForCategory(subCategory.Id);
        }

        private string LightenColor(string hexColor)
        {
            try
            {
                // Remove # if present
                hexColor = hexColor.TrimStart('#');
                
                // Parse RGB
                int r = Convert.ToInt32(hexColor.Substring(0, 2), 16);
                int g = Convert.ToInt32(hexColor.Substring(2, 2), 16);
                int b = Convert.ToInt32(hexColor.Substring(4, 2), 16);
                
                // Lighten by 40% (move toward white)
                r = (int)(r + (255 - r) * 0.4);
                g = (int)(g + (255 - g) * 0.4);
                b = (int)(b + (255 - b) * 0.4);
                
                return $"#{r:X2}{g:X2}{b:X2}";
            }
            catch
            {
                return "#E0E7FF"; // Default light blue
            }
        }

        private void LoadItemsForCategory(string categoryId)
        {
            System.Diagnostics.Debug.WriteLine($"[OrderPlacement] Loading items for category ID: {categoryId}");
            
            ItemsContainer.Children.Clear();
            
            var items = _allMenuItems
                .Where(i => i.CategoryId == categoryId)
                .OrderBy(i => i.DisplayOrder)
                .ToList();
            
            System.Diagnostics.Debug.WriteLine($"[OrderPlacement] Found {items.Count} items");
            
            foreach (var item in items)
            {
                var itemButton = CreateItemButton(item);
                ItemsContainer.Children.Add(itemButton);
            }
        }

        private Border CreateItemButton(FoodMenuItem item)
        {
            var border = new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#E2E8F0"),
                StrokeThickness = 1,
                Padding = 12,
                Margin = new Thickness(0, 0, 12, 12),
                WidthRequest = 160,
                HeightRequest = 120,
                StrokeShape = new RoundRectangle { CornerRadius = 10 }
            };
            
            var gesture = new TapGestureRecognizer();
            gesture.Tapped += (s, e) => AddItemToOrder(item);
            border.GestureRecognizers.Add(gesture);
            
            var stack = new VerticalStackLayout
            {
                Spacing = 8,
                VerticalOptions = LayoutOptions.Fill
            };
            
            stack.Children.Add(new Label
            {
                Text = item.Name,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1E293B"),
                LineBreakMode = LineBreakMode.WordWrap,
                MaxLines = 2
            });
            
            stack.Children.Add(new Label
            {
                Text = $"£{item.Price:F2}",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#10B981"),
                VerticalOptions = LayoutOptions.EndAndExpand
            });
            
            border.Content = stack;
            return border;
        }

        private void AddItemToOrder(FoodMenuItem item)
        {
            System.Diagnostics.Debug.WriteLine($"[OrderPlacement] Adding item: {item.Name}");
            
            // Check if item already exists in order
            var existingItem = _currentOrder.Items.FirstOrDefault(i => 
                i.MenuItemId == item.Id && 
                string.IsNullOrEmpty(i.Notes) && 
                i.SendStatus == ItemSendStatus.NotSent);
            
            if (existingItem != null)
            {
                // Increment quantity if item exists
                existingItem.Quantity++;
                System.Diagnostics.Debug.WriteLine($"[OrderPlacement] Increased quantity to {existingItem.Quantity}");
            }
            else
            {
                // Add new item
                var orderItem = new TableOrderItem
                {
                    Id = Guid.NewGuid().ToString(),
                    MenuItemId = item.Id ?? "",
                    Name = item.Name,
                    Quantity = 1,
                    UnitPrice = item.Price,
                    SendStatus = ItemSendStatus.NotSent,
                    CreatedAt = DateTime.Now
                };
                
                _currentOrder.Items.Add(orderItem);
            }
            
            _currentOrder.RecalculateAll();
            
            RefreshOrderItems();
            UpdateDisplay();
        }

        private void RefreshOrderItems()
        {
            OrderItemsContainer.Children.Clear();
            
            foreach (var item in _currentOrder.Items)
            {
                // Card container - more compact
                var itemView = new Border
                {
                    BackgroundColor = Color.FromArgb("#F9FAFB"),
                    StrokeThickness = 0,
                    Padding = new Thickness(8, 6),
                    Margin = new Thickness(0, 0, 0, 5),
                    HeightRequest = 40,
                    StrokeShape = new RoundRectangle { CornerRadius = 8 }
                };
                
                // Single-line grid layout
                var mainGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Item name
                        new ColumnDefinition { Width = GridLength.Auto }, // Minus button
                        new ColumnDefinition { Width = new GridLength(25, GridUnitType.Absolute) }, // Quantity
                        new ColumnDefinition { Width = GridLength.Auto }, // Plus button
                        new ColumnDefinition { Width = new GridLength(85, GridUnitType.Absolute) }, // Note button
                        new ColumnDefinition { Width = new GridLength(65, GridUnitType.Absolute) } // Price
                    },
                    ColumnSpacing = 6,
                    RowDefinitions = new RowDefinitionCollection
                    {
                        new RowDefinition { Height = GridLength.Auto }
                    }
                };
                
                // Item name
                var nameLabel = new Label
                {
                    Text = item.HasNotes ? $"{item.Name} 📝" : item.Name,
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#1E293B"),
                    VerticalOptions = LayoutOptions.Center,
                    LineBreakMode = LineBreakMode.TailTruncation
                };
                mainGrid.Add(nameLabel, 0, 0);
                
                // Minus button
                var minusBtn = new Button
                {
                    Text = "-",
                    BackgroundColor = Color.FromArgb("#EF4444"),
                    TextColor = Colors.White,
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    WidthRequest = 32,
                    HeightRequest = 32,
                    Padding = 0,
                    CornerRadius = 6,
                    VerticalOptions = LayoutOptions.Center
                };
                minusBtn.Clicked += (s, e) => {
                    if (item.Quantity > 1)
                    {
                        item.Quantity--;
                        _currentOrder.RecalculateAll();
                        RefreshOrderItems();
                        UpdateDisplay();
                    }
                    else
                    {
                        _currentOrder.Items.Remove(item);
                        _currentOrder.RecalculateAll();
                        RefreshOrderItems();
                        UpdateDisplay();
                    }
                };
                mainGrid.Add(minusBtn, 1, 0);
                
                // Quantity label
                var qtyLabel = new Label
                {
                    Text = item.Quantity.ToString(),
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#1E293B"),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                mainGrid.Add(qtyLabel, 2, 0);
                
                // Plus button
                var plusBtn = new Button
                {
                    Text = "+",
                    BackgroundColor = Color.FromArgb("#10B981"),
                    TextColor = Colors.White,
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    WidthRequest = 32,
                    HeightRequest = 32,
                    Padding = 0,
                    CornerRadius = 6,
                    VerticalOptions = LayoutOptions.Center
                };
                plusBtn.Clicked += (s, e) => {
                    item.Quantity++;
                    _currentOrder.RecalculateAll();
                    RefreshOrderItems();
                    UpdateDisplay();
                };
                mainGrid.Add(plusBtn, 3, 0);
                
                // Note button
                var noteBtn = new Button
                {
                    Text = item.HasNotes ? "✓ Note" : "+ Note",
                    BackgroundColor = Color.FromArgb("#3B82F6"),
                    TextColor = Colors.White,
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    HeightRequest = 32,
                    CornerRadius = 6,
                    Padding = new Thickness(8, 0),
                    VerticalOptions = LayoutOptions.Center
                };
                noteBtn.Clicked += async (s, e) => await ShowNoteDialog(item);
                mainGrid.Add(noteBtn, 4, 0);
                
                // Price
                var priceLabel = new Label
                {
                    Text = $"£{item.TotalPrice:F2}",
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#10B981"),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.End
                };
                mainGrid.Add(priceLabel, 5, 0);
                
                itemView.Content = mainGrid;
                OrderItemsContainer.Children.Add(itemView);
            }
        }
        
        private async Task ShowNoteDialog(TableOrderItem item)
        {
            var dialog = new StyledPromptDialog();
            dialog.SetDialog(
                "Add Note",
                $"Enter note for {item.Name}:",
                "e.g., No onions, extra spicy",
                null,
                item.Notes ?? ""
            );
            
            var result = await dialog.ShowAsync();
            
            if (result != null)
            {
                item.Notes = string.IsNullOrWhiteSpace(result) ? null : result;
                RefreshOrderItems();
            }
        }

        private void UpdateDisplay()
        {
            // Table number now shown in TopBar, not in right panel
            StaffNameLabel.Text = $"Staff: {_currentOrder.StaffName}";
            CoverCountLabel.Text = $"Covers: {_currentOrder.CoverCount}";
            
            SubtotalLabel.Text = $"£{_currentOrder.Subtotal:F2}";
            VATLabel.Text = $"£{_currentOrder.VAT:F2}";
            
            if (_currentOrder.ServiceChargePercent > 0)
            {
                ServiceChargeRow.IsVisible = true;
                ServiceChargeLabel.Text = $"£{_currentOrder.ServiceCharge:F2}";
            }
            else
            {
                ServiceChargeRow.IsVisible = false;
            }
            
            // Show discount if applied
            if (_currentOrder.Discount > 0)
            {
                DiscountRow.IsVisible = true;
                DiscountLabel.Text = $"-£{_currentOrder.Discount:F2}";
                if (!string.IsNullOrEmpty(_currentOrder.DiscountReason))
                {
                    DiscountReasonLabel.Text = $"Discount ({_currentOrder.DiscountReason})";
                }
                else
                {
                    DiscountReasonLabel.Text = "Discount";
                }
            }
            else
            {
                DiscountRow.IsVisible = false;
            }
            
            TotalLabel.Text = $"£{_currentOrder.Total:F2}";
        }

        private async void OnServiceFeeClicked(object? sender, EventArgs e)
        {
            var dialog = new ModernActionSheetDialog();
            dialog.SetActionSheet(
                "Service Charge",
                new List<string> { "None (0%)", "10%", "12.5%", "15%" },
                "🧾"
            );
            
            var action = await dialog.ShowAsync();
            
            if (action == "None (0%)")
                _currentOrder.ServiceChargePercent = 0;
            else if (action == "10%")
                _currentOrder.ServiceChargePercent = 10;
            else if (action == "12.5%")
                _currentOrder.ServiceChargePercent = 12.5m;
            else if (action == "15%")
                _currentOrder.ServiceChargePercent = 15;
            
            if (action != null)
            {
                _currentOrder.RecalculateAll();
                UpdateDisplay();
            }
        }

        private async void OnNotesClicked(object? sender, EventArgs e)
        {
            var dialog = new StyledPromptDialog();
            dialog.SetDialog(
                "Order Notes",
                "Enter notes for this order:",
                "e.g., Allergies, special requests...",
                null,
                _currentOrder.Notes ?? ""
            );
            
            var result = await dialog.ShowAsync();
            
            if (!string.IsNullOrEmpty(result))
            {
                _currentOrder.Notes = result;
                
                var successDialog = new ModernAlertDialog();
                successDialog.SetAlert("Notes Saved", "Order notes have been saved.", "✅", "#10B981", "White");
                await successDialog.ShowAsync();
            }
        }

        private async void OnVoidClicked(object? sender, EventArgs e)
        {
            if (_currentOrder.Items.Count == 0)
            {
                var noItemsDialog = new ModernAlertDialog();
                noItemsDialog.SetAlert("No Items", "There are no items to void.", "ℹ️");
                await noItemsDialog.ShowAsync();
                return;
            }
            
            // Show void reason selection
            var reasonDialog = new ModernActionSheetDialog();
            reasonDialog.SetActionSheet(
                "Void Reason",
                new List<string> { "Customer changed mind", "Wrong item entered", "Kitchen error", "Manager override" },
                "⚠️"
            );
            
            var reason = await reasonDialog.ShowAsync();
            
            if (reason == null)
                return; // User cancelled
            
            // Check if PIN required (for voids over £30 and not Manager/Admin)
            var authService = ServiceHelper.GetService<AuthenticationService>();
            var currentUser = authService?.CurrentUser;
            var requiresPin = _currentOrder.Total > 30 && 
                             (currentUser == null || currentUser.Role == UserRole.User);
            
            if (requiresPin)
            {
                var pinDialog = new StyledPromptDialog();
                pinDialog.SetDialog(
                    "Manager PIN Required",
                    $"Void amount £{_currentOrder.Total:F2} requires manager approval:",
                    "Enter PIN",
                    Keyboard.Numeric
                );
                
                var pin = await pinDialog.ShowAsync();
                
                if (string.IsNullOrEmpty(pin))
                    return; // User cancelled
                
                // TODO: Validate manager PIN
                // For now, accept any 4-digit PIN as placeholder
                if (pin.Length != 4)
                {
                    var errorDialog = new ModernAlertDialog();
                    errorDialog.SetAlert("Invalid PIN", "Please enter a valid 4-digit PIN.", "❌", "#EF4444", "White");
                    await errorDialog.ShowAsync();
                    return;
                }
            }
            
            var confirmDialog = new ModernConfirmDialog();
            confirmDialog.SetConfirm(
                "Void Order",
                $"Void this order?\nReason: {reason}\nAmount: £{_currentOrder.Total:F2}",
                "Yes",
                "No",
                "⚠️"
            );
            
            var confirm = await confirmDialog.ShowAsync();
            
            if (confirm)
            {
                _currentOrder.Items.Clear();
                _currentOrder.RecalculateAll();
                RefreshOrderItems();
                UpdateDisplay();
                
                var voidedDialog = new ModernAlertDialog();
                voidedDialog.SetAlert("Voided", $"Order has been voided.\nReason: {reason}", "✅", "#10B981", "White");
                await voidedDialog.ShowAsync();
                
                // Navigate back to Visual Table Layout
                await Shell.Current.GoToAsync("..");
            }
        }

        private async void OnSearchBarTapped(object? sender, TappedEventArgs e)
        {
            var keyboard = new VirtualKeyboardDialog();
            keyboard.SetInitialText(_searchQuery);
            
            var result = await keyboard.ShowAsync();
            
            if (result != null)
            {
                _searchQuery = result.Trim();
                UpdateSearchDisplay();
                FilterAndDisplayItems();
            }
        }

        private void UpdateSearchDisplay()
        {
            if (string.IsNullOrEmpty(_searchQuery))
            {
                SearchDisplayLabel.Text = "Search menu items...";
                SearchDisplayLabel.TextColor = Color.FromArgb("#94A3B8");
                ClearSearchButton.IsVisible = false;
            }
            else
            {
                SearchDisplayLabel.Text = _searchQuery;
                SearchDisplayLabel.TextColor = Color.FromArgb("#1E293B");
                ClearSearchButton.IsVisible = true;
            }
        }

        private void OnClearSearchClicked(object? sender, EventArgs e)
        {
            _searchQuery = string.Empty;
            UpdateSearchDisplay();
            FilterAndDisplayItems();
        }

        private void FilterAndDisplayItems()
        {
            ItemsContainer.Children.Clear();
            
            IEnumerable<FoodMenuItem> items;
            
            if (string.IsNullOrEmpty(_searchQuery))
            {
                // No search - show items from selected category
                if (_selectedCategory != null)
                {
                    items = _allMenuItems
                        .Where(i => i.CategoryId == _selectedCategory.Id)
                        .OrderBy(i => i.DisplayOrder);
                }
                else
                {
                    return;
                }
            }
            else
            {
                // Search across ALL items
                items = _allMenuItems
                    .Where(i => i.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(i => i.Name);
            }
            
            foreach (var item in items)
            {
                var itemButton = CreateItemButton(item);
                ItemsContainer.Children.Add(itemButton);
            }
        }

        private async void OnSendClicked(object? sender, EventArgs e)
        {
            if (_currentOrder.Items.Count == 0)
            {
                var noItemsDialog = new ModernAlertDialog();
                noItemsDialog.SetAlert("No Items", "Please add items before sending to kitchen.", "ℹ️");
                await noItemsDialog.ShowAsync();
                return;
            }
            
            foreach (var item in _currentOrder.Items.Where(i => i.SendStatus == ItemSendStatus.NotSent))
            {
                item.SendStatus = ItemSendStatus.Sent;
                item.SentAt = DateTime.Now;
            }
            
            _currentOrder.Status = TableOrderStatus.Sent;
            _currentOrder.UpdatedAt = DateTime.Now;
            
            var sentDialog = new ModernAlertDialog();
            sentDialog.SetAlert("Sent", $"{_currentOrder.Items.Count} items sent to kitchen!", "✅", "#10B981", "White");
            await sentDialog.ShowAsync();
        }

        private async void OnPrintReceiptClicked(object? sender, EventArgs e)
        {
            if (_currentOrder.Items.Count == 0)
            {
                var noItemsDialog = new ModernAlertDialog();
                noItemsDialog.SetAlert("No Items", "Please add items before printing receipt.", "ℹ️");
                await noItemsDialog.ShowAsync();
                return;
            }
            
            // TODO: Implement actual receipt printing logic here
            // For now, show a confirmation message
            var printDialog = new ModernAlertDialog();
            printDialog.SetAlert("Printing", $"Receipt for Table {_currentOrder.TableNumber} sent to printer!", "🖨️", "#6366F1", "White");
            await printDialog.ShowAsync();
        }

        private async void OnMoreClicked(object? sender, EventArgs e)
        {
            var dialog = new MoreOptionsDialog();
            
            // Check user role for restricted features
            var authService = ServiceHelper.GetService<AuthenticationService>();
            var currentUser = authService?.CurrentUser;
            bool isManagerOrAdmin = currentUser?.Role == UserRole.Manager || currentUser?.Role == UserRole.Admin;
            
            var options = new List<(string Text, string Icon, bool IsEnabled)>
            {
                ("Discount", "", true),
                ("Table Transfer", "", true),
                ("Merge Tables", "", true),
                ("Fire Course", "", _currentOrder.Items.Count > 0),
                ("Loyalty Points", "", true), // Now enabled
                ("Split Bill", "", _currentOrder.Items.Count > 0),
                ("Price Override", "", isManagerOrAdmin), // Manager/Admin only
                ("Cash Drawer", "", isManagerOrAdmin) // Manager/Admin only
            };
            
            dialog.SetOptions(options);
            var selected = await dialog.ShowAsync();
            
            if (selected != null)
            {
                switch (selected)
                {
                    case "Discount":
                        await ShowDiscountDialog();
                        break;
                    case "Table Transfer":
                        await ShowTableTransferDialog();
                        break;
                    case "Merge Tables":
                        await ShowMergeTablesDialog();
                        break;
                    case "Fire Course":
                        await ShowFireCourseDialog();
                        break;
                    case "Loyalty Points":
                        await ShowLoyaltyPointsDialog();
                        break;
                    case "Split Bill":
                        await ShowSplitBillDialog();
                        break;
                    case "Price Override":
                        await ShowPriceOverrideDialog();
                        break;
                    case "Cash Drawer":
                        await OpenCashDrawer();
                        break;
                }
            }
        }

        private async void OnPayClicked(object? sender, EventArgs e)
        {
            if (_currentOrder.Items.Count == 0)
            {
                var noItemsDialog = new ModernAlertDialog();
                noItemsDialog.SetAlert("No Items", "Please add items before proceeding to payment.", "ℹ️");
                await noItemsDialog.ShowAsync();
                return;
            }
            
            decimal tip = 0;
            decimal totalDue = _currentOrder.Total;
            
            // Step 1: Check if TIP should be shown (only if Service Charge = 0)
            if (_currentOrder.ServiceCharge == 0)
            {
                var tipDialog = new TipSelectionDialog();
                tipDialog.SetOrderTotal(_currentOrder.Subtotal);
                tip = await tipDialog.ShowAsync();
                
                if (tip == -1) // Cancelled
                {
                    return;
                }
                
                totalDue = _currentOrder.Subtotal + tip;
            }
            
            // Track remaining balance for partial payments
            decimal remainingBalance = totalDue;
            decimal totalPaid = 0;
            
            // Payment loop for partial payments
            while (remainingBalance > 0)
            {
                // Step 2: Show payment method selection
                var methodDialog = new PaymentMethodDialog();
                methodDialog.SetAmountDue(remainingBalance, totalDue - remainingBalance > 0 ? totalDue - remainingBalance : 0);
                var paymentMethod = await methodDialog.ShowAsync();
                
                if (paymentMethod == PaymentMethod.Cancelled)
                {
                    if (totalPaid > 0)
                    {
                        // Partial payment already made
                        var partialDialog = new ModernAlertDialog();
                        partialDialog.SetAlert("Partial Payment", $"£{totalPaid:F2} already paid. £{remainingBalance:F2} remaining.", "⚠️", "#F59E0B", "White");
                        await partialDialog.ShowAsync();
                    }
                    return;
                }
                
                // Step 3: Process selected payment method
                switch (paymentMethod)
                {
                    case PaymentMethod.Cash:
                        var cashResult = await ProcessCashPayment(remainingBalance);
                        if (cashResult.Success)
                        {
                            totalPaid += cashResult.AmountPaid;
                            remainingBalance = cashResult.Remaining;
                        }
                        break;
                        
                    case PaymentMethod.Card:
                        var cardResult = await ProcessCardPayment(remainingBalance);
                        if (cardResult.Success)
                        {
                            totalPaid += cardResult.AmountPaid;
                            remainingBalance = 0;
                        }
                        break;
                        
                    case PaymentMethod.GiftCard:
                        var giftResult = await ProcessGiftCardPayment(remainingBalance);
                        if (giftResult.Success)
                        {
                            totalPaid += giftResult.AmountApplied;
                            remainingBalance = giftResult.Remaining;
                        }
                        break;
                }
            }
            
            // Payment complete - print receipt and close table
            await CompletePayment(totalDue, tip, totalPaid);
        }

        private async Task<CashPaymentResult> ProcessCashPayment(decimal amountDue)
        {
            var cashDialog = new CashPaymentDialog();
            cashDialog.SetAmountDue(amountDue);
            return await cashDialog.ShowAsync();
        }

        private async Task<CardPaymentResult> ProcessCardPayment(decimal amountDue)
        {
            var cardDialog = new CardPaymentDialog();
            cardDialog.SetAmount(amountDue);
            return await cardDialog.ShowAsync();
        }

        private async Task<GiftCardPaymentResult> ProcessGiftCardPayment(decimal amountDue)
        {
            var giftDialog = new GiftCardPaymentDialog();
            giftDialog.SetAmountDue(amountDue);
            return await giftDialog.ShowAsync();
        }

        private async Task CompletePayment(decimal totalAmount, decimal tip, decimal totalPaid)
        {
            // Save order if it's a collection order
            if (_isCollectionOrder)
            {
                await SaveCollectionOrder(totalAmount, tip);
            }
            
            // Show payment success
            var successDialog = new ModernAlertDialog();
            string tipText = tip > 0 ? $"\nTip: £{tip:F2}" : "";
            string orderTypeText = _isCollectionOrder ? "\n\nCollection order saved!" : "";
            successDialog.SetAlert("Payment Complete", $"Total: £{totalAmount:F2}{tipText}{orderTypeText}\n\nPrinting receipt...", "✅", "#10B981", "White");
            await successDialog.ShowAsync();
            
            // Print receipt (always)
            await PrintReceipt(totalAmount, tip);
            
            // Close table and navigate back
            await CloseTable();
        }

        private async Task SaveCollectionOrder(decimal totalAmount, decimal tip)
        {
            try
            {
                var orderService = new OrderService();
                var customerService = new CollectionCustomerService();
                
                // Create order object
                var order = new Order
                {
                    OrderId = Guid.NewGuid().ToString(),
                    OrderNumber = $"COL-{DateTime.Now:yyyyMMdd-HHmmss}",
                    CustomerName = _collectionCustomerName,
                    CustomerPhone = _collectionCustomerPhone,
                    TotalAmount = totalAmount,
                    SubtotalAmount = _currentOrder.Subtotal,
                    TaxAmount = _currentOrder.VAT,
                    OrderType = "collection",
                    PaymentMethod = "paid",
                    Status = OrderStatus.Completed,
                    CompletedTime = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    SyncStatus = POS_in_NET.Models.SyncStatus.Synced,
                    Items = new List<OrderItem>()
                };
                
                // Convert TableOrderItems to OrderItems
                foreach (var item in _currentOrder.Items)
                {
                    order.Items.Add(new OrderItem
                    {
                        ItemName = item.Name,
                        Quantity = item.Quantity,
                        ItemPrice = item.UnitPrice,
                        SpecialInstructions = item.Notes
                    });
                }
                
                // Save order
                await orderService.SaveOrderAsync(order);
                
                // Update customer last order date
                await customerService.UpdateLastOrderDateAsync(_collectionCustomerId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving collection order: {ex.Message}");
            }
        }

        private async Task PrintReceipt(decimal total, decimal tip)
        {
            // TODO: Implement actual receipt printing
            // For now, simulate print delay
            await Task.Delay(500);
            
            // In real implementation:
            // - Connect to printer service
            // - Format receipt with order items, totals, tip, etc.
            // - Send to printer
        }

        private async Task CloseTable()
        {
            // Stop timer
            _timer?.Stop();
            _timer?.Dispose();
            
            // TODO: Update database - mark order as paid, close table session
            
            // Navigate back based on order type
            if (_isCollectionOrder)
            {
                // Navigate back to dashboard for collection orders
                await Shell.Current.GoToAsync("//dashboard");
            }
            else
            {
                // Navigate back to Visual Table Layout for dine-in orders
                await Shell.Current.GoToAsync("//VisualTablePage");
            }
        }

        private async Task ShowDiscountDialog()
        {
            var dialog = new DiscountDialog();
            dialog.SetOrderSubtotal(_currentOrder.Subtotal);
            
            var result = await dialog.ShowAsync();
            
            if (result.HasValue)
            {
                var (discountAmount, discountPercent, reason) = result.Value;
                
                _currentOrder.Discount = discountAmount;
                _currentOrder.DiscountPercent = discountPercent;
                _currentOrder.DiscountReason = reason;
                
                UpdateDisplay();
                
                if (discountAmount > 0)
                {
                    var alert = new ModernAlertDialog();
                    alert.SetAlert("Discount Applied", $"£{discountAmount:F2} discount applied.\nReason: {reason}", "", "#10B981", "White");
                    await alert.ShowAsync();
                }
                else if (discountAmount == 0 && string.IsNullOrEmpty(reason))
                {
                    // Discount was removed
                    var alert = new ModernAlertDialog();
                    alert.SetAlert("Discount Removed", "Discount has been removed from the order.", "", "#3B82F6", "White");
                    await alert.ShowAsync();
                }
            }
        }

        private async Task ShowTableTransferDialog()
        {
            var dialog = new TableTransferDialog();
            dialog.SetCurrentTable(_currentOrder.TableNumber.ToString());
            
            var selectedTable = await dialog.ShowAsync();
            
            if (selectedTable != null)
            {
                // Transfer the order to the new table
                int oldTableNumber = _currentOrder.TableNumber;
                _currentOrder.TableNumber = int.Parse(selectedTable.TableNumber);
                
                // Update TopBar title
                if (TopBar != null)
                {
                    TopBar.SetPageTitle($"Table {selectedTable.TableNumber}");
                }
                
                // Show success message
                var alert = new ModernAlertDialog();
                alert.SetAlert(
                    "Transfer Complete", 
                    $"Order transferred from Table {oldTableNumber} to Table {selectedTable.TableNumber}", 
                    "", 
                    "#4CAF50", 
                    "White"
                );
                await alert.ShowAsync();
                
                // TODO: Update table status in database
                // - Mark old table as Available
                // - Mark new table as Occupied
            }
        }

        private async Task ShowMergeTablesDialog()
        {
            var infoDialog = new ModernAlertDialog();
            infoDialog.SetAlert("Merge Tables", "Merge tables feature coming soon!", "🔗", "#3B82F6", "White");
            await infoDialog.ShowAsync();
        }

        private async Task ShowFireCourseDialog()
        {
            var dialog = new FireCourseDialog();
            
            var selected = await dialog.ShowAsync();
            
            if (selected != null)
            {
                string message = selected == "All" 
                    ? "All courses sent to kitchen!" 
                    : $"{selected} sent to kitchen!";
                    
                var successDialog = new ModernAlertDialog();
                successDialog.SetAlert("Course Fired", message, "", "#10B981", "White");
                await successDialog.ShowAsync();
            }
        }

        private async Task ShowSplitBillDialog()
        {
            var dialog = new ModernActionSheetDialog();
            dialog.SetActionSheet(
                "Split Bill",
                new List<string> { "Split by Items", "Split by 2", "Split by 4", "Split by 6" },
                "✂️"
            );
            
            var selected = await dialog.ShowAsync();
            
            if (selected != null)
            {
                var successDialog = new ModernAlertDialog();
                successDialog.SetAlert("Bill Split", $"Bill split: {selected}", "✅", "#10B981", "White");
                await successDialog.ShowAsync();
            }
        }

        private async Task ShowLoyaltyPointsDialog()
        {
            var dialog = new StyledPromptDialog();
            dialog.SetDialog(
                "Loyalty Points",
                "Enter customer phone or loyalty card number:",
                "e.g., 555-1234",
                Keyboard.Telephone
            );
            
            var result = await dialog.ShowAsync();
            
            if (!string.IsNullOrEmpty(result))
            {
                var infoDialog = new ModernAlertDialog();
                infoDialog.SetAlert("Loyalty", "Loyalty points feature coming soon!", "⭐", "#3B82F6", "White");
                await infoDialog.ShowAsync();
            }
        }

        private async Task ShowPriceOverrideDialog()
        {
            if (_currentOrder.Items.Count == 0)
            {
                var noItemsDialog = new ModernAlertDialog();
                noItemsDialog.SetAlert("No Items", "Please add items before overriding prices.", "ℹ️");
                await noItemsDialog.ShowAsync();
                return;
            }
            
            var infoDialog = new ModernAlertDialog();
            infoDialog.SetAlert("Price Override", "Select an item to override its price.", "💵", "#3B82F6", "White");
            await infoDialog.ShowAsync();
        }

        private async Task OpenCashDrawer()
        {
            var confirmDialog = new ModernConfirmDialog();
            confirmDialog.SetConfirm(
                "Open Cash Drawer",
                "Are you sure you want to open the cash drawer?",
                "Yes",
                "No",
                "💳"
            );
            
            var confirm = await confirmDialog.ShowAsync();
            
            if (confirm)
            {
                // TODO: Send command to cash drawer hardware
                var successDialog = new ModernAlertDialog();
                successDialog.SetAlert("Cash Drawer", "Cash drawer opened successfully!", "✅", "#10B981", "White");
                await successDialog.ShowAsync();
            }
        }
    }
}

