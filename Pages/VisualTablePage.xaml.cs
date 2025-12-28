using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using POS_in_NET.Models;
using POS_in_NET.Services;

namespace POS_in_NET.Pages
{
    public partial class VisualTablePage : ContentPage
    {
        private readonly FloorService _floorService;
        private readonly RestaurantTableService _tableService;
        private readonly TableSessionService _sessionService;
        private List<Floor> _floors = new();
        private Floor? _currentFloor;
        private RestaurantTable? _selectedTable;
        private Dictionary<int, Border> _tableViews = new();
        private bool _isAdmin = true; // TODO: Get from auth service
        private bool _hasUnsavedChanges = false;
        private const int GRID_SIZE = 20; // 20px snap grid

        public VisualTablePage()
        {
            InitializeComponent();
            
            TopBar.SetPageTitle("Restaurant Layout");
            
            _floorService = new FloorService();
            _tableService = new RestaurantTableService();
            _sessionService = new TableSessionService();
            
            // Set up numeric keyboard event
            NumericKeyboard.NumberConfirmed += OnNumericKeyboardConfirmed;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadFloorsAndTables();
            UpdateAdminToolsVisibility();
        }

        private void UpdateAdminToolsVisibility()
        {
            SetBackgroundButton.IsVisible = _isAdmin;
            SaveLayoutButton.IsVisible = _isAdmin;
            // Show remove button only if there's a background image
            UpdateRemoveBackgroundButtonVisibility();
        }
        
        private void UpdateRemoveBackgroundButtonVisibility()
        {
            RemoveBackgroundButton.IsVisible = _isAdmin && _currentFloor != null && !string.IsNullOrEmpty(_currentFloor.BackgroundImage);
        }

        private async Task LoadFloorsAndTables()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== LoadFloorsAndTables Started ===");
                
                _floors = await _floorService.GetAllFloorsAsync();
                
                System.Diagnostics.Debug.WriteLine($"Floors returned: {_floors.Count}");
                
                if (_floors.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No floors found - showing empty state");
                    ShowNoFloorsMessage();
                    await ToastNotification.ShowAsync("Info", "No floors found. Please add floors in Table Management.", NotificationType.Info);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Creating floor tabs for {_floors.Count} floors");
                CreateFloorTabs();
                
                System.Diagnostics.Debug.WriteLine($"Selecting first floor: {_floors.First().Name}");
                await SelectFloor(_floors.First());
                
                System.Diagnostics.Debug.WriteLine("=== LoadFloorsAndTables Completed ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! Error loading floors: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                await ToastNotification.ShowAsync("Error", $"Could not load floors: {ex.Message}", NotificationType.Error);
            }
        }

        private void CreateFloorTabs()
        {
            FloorTabsLayout.Children.Clear();
            
            foreach (var floor in _floors)
            {
                var tabBorder = new Border
                {
                    BackgroundColor = Color.FromArgb("#F3F4F6"),
                    Stroke = Color.FromArgb("#E5E7EB"),
                    StrokeThickness = 1,
                    Padding = new Thickness(16, 10),
                    StrokeShape = new RoundRectangle { CornerRadius = 20 }
                };

                var tabLabel = new Label
                {
                    Text = $"{floor.Name} ({floor.TableCount})",
                    FontSize = 14,
                    FontFamily = "OpenSansSemibold",
                    TextColor = Color.FromArgb("#374151"),
                    VerticalOptions = LayoutOptions.Center
                };

                tabBorder.Content = tabLabel;
                
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => await SelectFloor(floor);
                tabBorder.GestureRecognizers.Add(tapGesture);
                
                FloorTabsLayout.Children.Add(tabBorder);
            }
        }

        private async Task SelectFloor(Floor floor)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== SelectFloor: {floor.Name} (ID: {floor.Id}) ===");
                
                _currentFloor = floor;
                UpdateFloorTabAppearance();
                
                // Load floor background if exists
                if (!string.IsNullOrEmpty(floor.BackgroundImage))
                {
                    System.Diagnostics.Debug.WriteLine($"Setting background: {floor.BackgroundImage}");
                    CanvasBackgroundImage.Source = floor.BackgroundImage;
                    CanvasBackgroundImage.IsVisible = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No background image");
                    CanvasBackgroundImage.Source = null;
                    CanvasBackgroundImage.IsVisible = false;
                }
                
                // Update remove button visibility based on current floor's background
                UpdateRemoveBackgroundButtonVisibility();
                
                // Load tables for this floor
                System.Diagnostics.Debug.WriteLine($"Loading tables for floor {floor.Id}...");
                var tables = await _tableService.GetTablesByFloorAsync(floor.Id);
                System.Diagnostics.Debug.WriteLine($"Tables loaded: {tables.Count}");
                
                ClearTableViews();
                
                if (tables.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No tables on this floor - showing empty state");
                    EmptyStateView.IsVisible = true;
                }
                else
                {
                    EmptyStateView.IsVisible = false;
                    System.Diagnostics.Debug.WriteLine("Creating table views...");
                    CreateTableViews(tables);
                    System.Diagnostics.Debug.WriteLine($"Table views created: {_tableViews.Count}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! Error selecting floor: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                await ToastNotification.ShowAsync("Error", $"Could not load tables: {ex.Message}", NotificationType.Error);
            }
        }

        private void UpdateFloorTabAppearance()
        {
            if (_currentFloor == null) return;
            
            foreach (var child in FloorTabsLayout.Children.OfType<Border>())
            {
                var label = child.Content as Label;
                if (label != null && label.Text.StartsWith(_currentFloor.Name))
                {
                    child.BackgroundColor = Color.FromArgb("#3B82F6");
                    child.Stroke = Colors.Transparent;
                    label.TextColor = Colors.White;
                }
                else
                {
                    child.BackgroundColor = Color.FromArgb("#F3F4F6");
                    child.Stroke = Color.FromArgb("#E5E7EB");
                    if (label != null) label.TextColor = Color.FromArgb("#374151");
                }
            }
        }

        private void ClearTableViews()
        {
            foreach (var tableView in _tableViews.Values)
            {
                TableCanvas.Children.Remove(tableView);
            }
            _tableViews.Clear();
            _selectedTable = null;
        }

        private void CreateTableViews(List<RestaurantTable> tables)
        {
            int defaultX = 40;
            int defaultY = 40;
            int index = 0;
            
            foreach (var table in tables)
            {
                var tableView = CreateTableView(table);
                
                // Use saved position or calculate default grid position
                int posX = table.PositionX > 0 ? table.PositionX : defaultX + (index % 6) * 140;
                int posY = table.PositionY > 0 ? table.PositionY : defaultY + (index / 6) * 140;
                
                AbsoluteLayout.SetLayoutBounds(tableView, new Rect(posX, posY, 120, 120));
                AbsoluteLayout.SetLayoutFlags(tableView, AbsoluteLayoutFlags.None);
                
                TableCanvas.Children.Add(tableView);
                _tableViews[table.Id] = tableView;
                index++;
            }
        }

        private Border CreateTableView(RestaurantTable table)
        {
            // Get colors based on status
            var (bgColor, borderColor, textColor) = GetTableColors(table.Status);
            
            var tableBorder = new Border
            {
                BackgroundColor = bgColor,
                Stroke = borderColor,
                StrokeThickness = 2,
                Padding = new Thickness(8),
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Shadow = new Shadow
                {
                    Brush = Brush.Black,
                    Offset = new Point(2, 2),
                    Radius = 8,
                    Opacity = 0.15f
                }
            };

            // Content
            var contentStack = new VerticalStackLayout
            {
                Spacing = 4,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            // Table image
            var tableImage = new Image
            {
                Source = table.TableDesignIcon,
                WidthRequest = 50,
                HeightRequest = 50,
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.Center
            };

            // Table number
            var tableNumber = new Label
            {
                Text = table.TableNumber,
                FontSize = 16,
                FontFamily = "OpenSansSemibold",
                FontAttributes = FontAttributes.Bold,
                TextColor = textColor,
                HorizontalOptions = LayoutOptions.Center
            };

            // Status indicator
            var statusDot = new Ellipse
            {
                WidthRequest = 10,
                HeightRequest = 10,
                Fill = borderColor,
                HorizontalOptions = LayoutOptions.Center
            };

            contentStack.Children.Add(tableImage);
            contentStack.Children.Add(tableNumber);
            contentStack.Children.Add(statusDot);

            tableBorder.Content = contentStack;

            // Store table reference
            tableBorder.BindingContext = table;

            // Add drag gesture for admin FIRST (higher priority)
            if (_isAdmin)
            {
                var panGesture = new PanGestureRecognizer();
                panGesture.PanUpdated += (s, e) => OnTableDrag(table, tableBorder, e);
                tableBorder.GestureRecognizers.Add(panGesture);
            }

            // Add tap gesture for selection
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => SelectTable(table, tableBorder);
            tableBorder.GestureRecognizers.Add(tapGesture);

            return tableBorder;
        }

        private (Color bg, Color border, Color text) GetTableColors(TableStatus status)
        {
            return status switch
            {
                TableStatus.Available => (Color.FromArgb("#D1FAE5"), Color.FromArgb("#10B981"), Color.FromArgb("#065F46")),
                TableStatus.Occupied => (Color.FromArgb("#DBEAFE"), Color.FromArgb("#3B82F6"), Color.FromArgb("#1E40AF")),
                TableStatus.Reserved => (Color.FromArgb("#EDE9FE"), Color.FromArgb("#8B5CF6"), Color.FromArgb("#5B21B6")),
                _ => (Color.FromArgb("#FEF3C7"), Color.FromArgb("#F59E0B"), Color.FromArgb("#92400E"))
            };
        }

        private double _startX, _startY;
        private double _dragStartX, _dragStartY;
        
        private void OnTableDrag(RestaurantTable table, Border tableView, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    var bounds = AbsoluteLayout.GetLayoutBounds(tableView);
                    _startX = bounds.X;
                    _startY = bounds.Y;
                    _dragStartX = bounds.X;
                    _dragStartY = bounds.Y;
                    tableView.Scale = 1.05;
                    tableView.Opacity = 0.8;
                    System.Diagnostics.Debug.WriteLine($"Drag started at ({_startX}, {_startY})");
                    break;

                case GestureStatus.Running:
                    var newX = _startX + e.TotalX;
                    var newY = _startY + e.TotalY;
                    
                    // Get actual canvas dimensions
                    double canvasWidth = TableCanvas.Width > 0 ? TableCanvas.Width : 1200;
                    double canvasHeight = TableCanvas.Height > 0 ? TableCanvas.Height : 800;
                    
                    // Keep within canvas bounds
                    newX = Math.Max(0, Math.Min(newX, canvasWidth - 120));
                    newY = Math.Max(0, Math.Min(newY, canvasHeight - 120));
                    
                    AbsoluteLayout.SetLayoutBounds(tableView, new Rect(newX, newY, 120, 120));
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    tableView.Scale = 1.0;
                    tableView.Opacity = 1.0;
                    
                    // Snap to grid
                    var finalBounds = AbsoluteLayout.GetLayoutBounds(tableView);
                    int snappedX = (int)(Math.Round(finalBounds.X / GRID_SIZE) * GRID_SIZE);
                    int snappedY = (int)(Math.Round(finalBounds.Y / GRID_SIZE) * GRID_SIZE);
                    
                    AbsoluteLayout.SetLayoutBounds(tableView, new Rect(snappedX, snappedY, 120, 120));
                    
                    // Update table position
                    table.PositionX = snappedX;
                    table.PositionY = snappedY;
                    
                    System.Diagnostics.Debug.WriteLine($"Drag completed: ({_dragStartX}, {_dragStartY}) -> ({snappedX}, {snappedY})");
                    
                    // Only mark as changed if position actually changed
                    if (Math.Abs(_dragStartX - snappedX) > 1 || Math.Abs(_dragStartY - snappedY) > 1)
                    {
                        SetUnsavedChanges(true);
                        // Auto-save
                        _ = AutoSaveTablePositionAsync(table.Id, snappedX, snappedY);
                    }
                    break;
            }
        }

        private void SetUnsavedChanges(bool hasChanges)
        {
            _hasUnsavedChanges = hasChanges;
            UnsavedChangesBar.IsVisible = hasChanges;
        }

        private async Task AutoSaveTablePositionAsync(int tableId, int x, int y)
        {
            try
            {
                var success = await _tableService.UpdateTablePositionAsync(tableId, x, y);
                if (success)
                {
                    SetUnsavedChanges(false);
                    System.Diagnostics.Debug.WriteLine($"Auto-saved table {tableId} position to ({x}, {y})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-save failed: {ex.Message}");
            }
        }

        private RestaurantTable? _popupTable;
        private Border? _popupTableView;

        private void SelectTable(RestaurantTable table, Border tableView)
        {
            // Store references for later
            _popupTable = table;
            _popupTableView = tableView;
            
            // Highlight the selected table
            tableView.BackgroundColor = Color.FromArgb("#3B82F6");
            tableView.Stroke = Color.FromArgb("#1D4ED8");
            
            // Show custom cover popup
            CoverPopupTitle.Text = $"Table {table.TableNumber}";
            // Reset custom cover input
            _customCoverValue = 0;
            CustomCoverLabel.Text = "Other number...";
            CustomCoverLabel.TextColor = Color.FromArgb("#9CA3AF");
            CoverPopupOverlay.IsVisible = true;
        }
        
        private void OnCloseCoverPopup(object sender, EventArgs e)
        {
            CloseCoverPopup();
        }
        
        private void CloseCoverPopup()
        {
            CoverPopupOverlay.IsVisible = false;
            
            // Reset table appearance
            if (_popupTable != null && _popupTableView != null)
            {
                var (bg, border, _) = GetTableColors(_popupTable.Status);
                _popupTableView.BackgroundColor = bg;
                _popupTableView.Stroke = border;
            }
            
            _popupTable = null;
            _popupTableView = null;
        }
        
        private async void OnCoverSelected(object sender, EventArgs e)
        {
            if (sender is Button button && _popupTable != null)
            {
                if (int.TryParse(button.Text, out int coverCount))
                {
                    await ProcessTableSelection(coverCount);
                }
            }
        }
        
        private int _customCoverValue = 0;
        
        private void OnCustomCoverTapped(object sender, EventArgs e)
        {
            // Show numeric keyboard
            NumericKeyboard.Show();
        }
        
        private async void OnNumericKeyboardConfirmed(object? sender, int number)
        {
            _customCoverValue = number;
            CustomCoverLabel.Text = number.ToString();
            CustomCoverLabel.TextColor = Color.FromArgb("#1F2937");
            
            // Automatically proceed with selection
            if (_popupTable != null)
            {
                await ProcessTableSelection(number);
            }
        }
        
        private async void OnCustomCoverSubmit(object sender, EventArgs e)
        {
            if (_popupTable == null) return;
            
            if (_customCoverValue <= 0)
            {
                // Show numeric keyboard if no value entered
                NumericKeyboard.Show();
                return;
            }
            
            await ProcessTableSelection(_customCoverValue);
        }
        
        private async Task ProcessTableSelection(int coverCount)
        {
            if (_popupTable == null) return;
            
            var tableNumber = _popupTable.TableNumber;
            var tableId = _popupTable.Id;
            
            // Close popup first
            CoverPopupOverlay.IsVisible = false;
            
            System.Diagnostics.Debug.WriteLine($"Table {tableNumber} selected with {coverCount} covers");
            
            // Reset table view
            if (_popupTableView != null)
            {
                var (bg, border, _) = GetTableColors(_popupTable.Status);
                _popupTableView.BackgroundColor = bg;
                _popupTableView.Stroke = border;
            }
            
            _popupTable = null;
            _popupTableView = null;
            
            // Navigate to Order Placement page IMMEDIATELY - no animation for instant feel
            var orderPage = new OrderPlacementPageSimple(tableNumber, coverCount, "Current User", 1);
            await Navigation.PushAsync(orderPage, animated: false);
        }

        private void ShowNoFloorsMessage()
        {
            EmptyStateView.IsVisible = true;
        }

        // Event Handlers
        private async void OnSetBackgroundClicked(object sender, EventArgs e)
        {
            if (_currentFloor == null) return;

            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select Floor Background Image",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    // Copy file to app data folder
                    var appDataPath = FileSystem.AppDataDirectory;
                    var fileName = $"floor_{_currentFloor.Id}_bg{System.IO.Path.GetExtension(result.FileName)}";
                    var destPath = System.IO.Path.Combine(appDataPath, fileName);
                    
                    using var sourceStream = await result.OpenReadAsync();
                    using var destStream = File.Create(destPath);
                    await sourceStream.CopyToAsync(destStream);

                    // Update database
                    await _floorService.UpdateFloorBackgroundAsync(_currentFloor.Id, destPath);
                    
                    // Update UI
                    _currentFloor.BackgroundImage = destPath;
                    CanvasBackgroundImage.Source = destPath;
                    CanvasBackgroundImage.IsVisible = true;
                    UpdateRemoveBackgroundButtonVisibility();

                    await ToastNotification.ShowAsync("Success", "Background image updated", NotificationType.Success);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting background: {ex.Message}");
                await ToastNotification.ShowAsync("Error", "Could not set background image", NotificationType.Error);
            }
        }

        private async void OnSaveLayoutClicked(object sender, EventArgs e)
        {
            try
            {
                int savedCount = 0;
                foreach (var kvp in _tableViews)
                {
                    var bounds = AbsoluteLayout.GetLayoutBounds(kvp.Value);
                    var success = await _tableService.UpdateTablePositionAsync(kvp.Key, (int)bounds.X, (int)bounds.Y);
                    if (success) savedCount++;
                }

                SetUnsavedChanges(false);
                await ToastNotification.ShowAsync("Success", $"Layout saved ({savedCount} tables)", NotificationType.Success);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving layout: {ex.Message}");
                await ToastNotification.ShowAsync("Error", "Could not save layout", NotificationType.Error);
            }
        }

        private async void OnRemoveBackgroundClicked(object sender, EventArgs e)
        {
            if (_currentFloor == null) return;

            try
            {
                // Remove from database
                await _floorService.UpdateFloorBackgroundAsync(_currentFloor.Id, string.Empty);
                
                // Update UI
                _currentFloor.BackgroundImage = string.Empty;
                CanvasBackgroundImage.Source = null;
                CanvasBackgroundImage.IsVisible = false;
                UpdateRemoveBackgroundButtonVisibility();

                await ToastNotification.ShowAsync("Success", "Background image removed", NotificationType.Success);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing background: {ex.Message}");
                await ToastNotification.ShowAsync("Error", "Could not remove background image", NotificationType.Error);
            }
        }

        private async void OnGoToTableManagementClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//table");
        }
    }
}
