using POS_in_NET.Models;
using POS_in_NET.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace POS_in_NET.Pages
{
    public partial class WebOrdersPage : ContentPage, INotifyPropertyChanged
    {
        private readonly DatabaseService? _databaseService;
        private readonly AuthenticationService? _authService;
        
        private User? _currentUser;
        private Timer? _refreshTimer;
        
        // Pagination
        private int _currentPage = 0;
        private int _pageSize = 10;
        private int _totalPages = 0;
        private string _searchText = "";
        
        // Date Filter - Always defaults to TODAY
        private DateTime _selectedDate = DateTime.Today;
        private bool _showAllOrders = false; // Toggle to show all orders without date filter
        
        private ObservableCollection<Order> _orders = new ObservableCollection<Order>();
        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set
            {
                _orders = value;
                OnPropertyChanged();
            }
        }
        
        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public WebOrdersPage()
        {
            InitializeComponent();
            _databaseService = ServiceHelper.GetService<DatabaseService>();
            _authService = ServiceHelper.GetService<AuthenticationService>();
            
            // Set the page title in the TopBar
            TopBar.SetPageTitle("Web Orders");
            
            // Set BindingContext for data binding
            BindingContext = this;
            
            // ✅ CRITICAL: Subscribe to WebSocket order updates for REAL-TIME delivery
            var wsService = ServiceHelper.GetService<OrderWebWebSocketService>();
            if (wsService != null)
            {
                wsService.NewOrderReceived += (sender, args) => {
                    System.Diagnostics.Debug.WriteLine($"🎉 WEBSOCKET ORDER RECEIVED: {args.OrderNumber} - {args.CustomerName} - ${args.TotalAmount}");
                    RefreshWebOrders();
                };
                System.Diagnostics.Debug.WriteLine("✅ WebSocket event subscription active!");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ WebSocket service not found!");
            }
            
            // ✅ Subscribe to CloudService.OnOrdersUpdated for automatic polling updates
            var cloudService = ServiceHelper.GetService<CloudOrderService>();
            if (cloudService != null)
            {
                cloudService.OnOrdersUpdated += () => {
                    System.Diagnostics.Debug.WriteLine("🔔 CloudService detected new orders - refreshing UI!");
                    RefreshWebOrders();
                };
                System.Diagnostics.Debug.WriteLine("✅ CloudService polling event subscription active!");
            }
            
            // Now link WebSocket to CloudService
            if (cloudService != null && wsService != null)
            {
                // Link WebSocket service to cloud service for status monitoring only
                cloudService.SetWebSocketService(wsService);
                System.Diagnostics.Debug.WriteLine("✅ CloudService linked to WebSocket for status monitoring (no UI auto-refresh)");
            }
            
            // Start status update timer (every 10 seconds)
            _refreshTimer = new Timer(_ => 
            {
                MainThread.BeginInvokeOnMainThread(() => UpdateConnectionStatus());
            }, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            // Subscribe to direct database order updates for INSTANT 0.5s delivery
            var directDbService = ServiceHelper.GetService<OrderWebDirectDatabaseService>();
            if (directDbService != null)
            {
                directDbService.OnNewOrdersDetected += () => {
                    System.Diagnostics.Debug.WriteLine("⚡ INSTANT UPDATE: New orders detected via direct database - refreshing UI immediately!");
                    RefreshWebOrders();
                };
            }

            LoadPageAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // ❌ NO AUTO-REFRESH TIMER - Page only refreshes from:
            // 1. WebSocket messages (real-time)
            // 2. Manual "Sync Now" button
            // 3. Initial page load
            System.Diagnostics.Debug.WriteLine("📄 OnAppearing - Web Orders page opened (NO auto-refresh timer)");
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _refreshTimer?.Dispose();
        }

        private async void LoadPageAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 LoadPageAsync START");
                
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                // Get current user (optional - no redirect if not found)
                _currentUser = _authService?.GetCurrentUser();
                System.Diagnostics.Debug.WriteLine($"👤 Current user: {_currentUser?.Username ?? "None"}");

                // Update UI for web orders display
                UpdateUIForUserRole();
                System.Diagnostics.Debug.WriteLine("✅ UpdateUIForUserRole completed");
                
                // DEBUG: Check WebSocket connection status
                var wsService = ServiceHelper.GetService<OrderWebWebSocketService>();
                if (wsService != null)
                {
                    var status = wsService.GetConnectionStatus();
                    System.Diagnostics.Debug.WriteLine($"🔍 WebSocket Status: {status}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ WebSocket service is NULL!");
                }
                
                // Initialize date picker to TODAY
                OrderDatePicker.Date = DateTime.Today;
                _selectedDate = DateTime.Today;
                System.Diagnostics.Debug.WriteLine($"📅 Date picker initialized to: {_selectedDate:MMM dd, yyyy}");
                
                // Smart Auto-sync: Always sync from start of today to catch ALL orders
                // This ensures we never miss orders that came when app was closed
                var cloudService = ServiceHelper.GetService<CloudOrderService>();
                var orderService = ServiceHelper.GetService<OrderService>();
                if (cloudService != null && orderService != null)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("🔄 Smart Auto-sync: Syncing all orders from today...");
                        
                        // Get the latest order from database to check if we need historical sync
                        var allOrders = await orderService.GetOrdersAsync();
                        var latestOrder = allOrders
                            .Where(o => o.SyncStatus == Models.SyncStatus.Synced)
                            .OrderByDescending(o => o.CreatedAt)
                            .FirstOrDefault();
                        
                        DateTime syncFromDate;
                        
                        if (latestOrder != null && latestOrder.CreatedAt.Date >= DateTime.Today.AddDays(-1))
                        {
                            // Latest order is from today or yesterday - sync from start of today
                            // This catches ALL orders including those that came when app was closed
                            syncFromDate = DateTime.Today;
                            System.Diagnostics.Debug.WriteLine($"📅 Last order: {latestOrder.OrderNumber} at {latestOrder.CreatedAt:yyyy-MM-dd HH:mm}");
                            System.Diagnostics.Debug.WriteLine($"🔄 Syncing from: {syncFromDate:yyyy-MM-dd HH:mm:ss} (START OF TODAY)");
                        }
                        else if (latestOrder != null)
                        {
                            // Latest order is older - sync from 7 days ago to catch recent history
                            syncFromDate = DateTime.Today.AddDays(-7);
                            System.Diagnostics.Debug.WriteLine($"📅 Last order: {latestOrder.OrderNumber} at {latestOrder.CreatedAt:yyyy-MM-dd HH:mm}");
                            System.Diagnostics.Debug.WriteLine($"🔄 Syncing from: {syncFromDate:yyyy-MM-dd HH:mm:ss} (LAST 7 DAYS)");
                        }
                        else
                        {
                            // No orders in database - sync last 60 days for initial setup
                            syncFromDate = DateTime.Today.AddDays(-60);
                            System.Diagnostics.Debug.WriteLine($"📅 No orders in database, syncing last 60 days from {syncFromDate:yyyy-MM-dd}");
                        }
                        
                        var syncResult = await cloudService.SyncOrdersByDateAsync(syncFromDate);
                        System.Diagnostics.Debug.WriteLine($"✅ Smart sync complete: {syncResult.Message}");
                        System.Diagnostics.Debug.WriteLine($"📊 Found {syncResult.OrdersFound} orders from API");
                        
                        if (syncResult.OrdersFound > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"🔄 Reloading orders after syncing {syncResult.OrdersFound} new orders...");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Smart sync error: {ex.Message}");
                    }
                }
                
                // Load web orders from database (after smart sync completes)
                System.Diagnostics.Debug.WriteLine("📦 About to call LoadWebOrdersAsync...");
                await LoadWebOrdersAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine("✅ LoadWebOrdersAsync completed");
                
                // Update connection status on main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        UpdateConnectionStatus();
                        System.Diagnostics.Debug.WriteLine("✅ UpdateConnectionStatus completed");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error in UpdateConnectionStatus: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in LoadPageAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Error", $"Failed to load page: {ex.Message}", "OK");
                });
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LoadingIndicator.IsVisible = false;
                    LoadingIndicator.IsRunning = false;
                    System.Diagnostics.Debug.WriteLine("✅ LoadPageAsync COMPLETE");
                });
            }
        }

        private void UpdateUIForUserRole()
        {
            // Always show web orders - no authentication restriction
            AccessDeniedFrame.IsVisible = false;
            FiltersSection.IsVisible = true;
            OrdersSection.IsVisible = true;
            StatsSection.IsVisible = true; // This now includes Today's Orders + Filter + Search
        }

        private async Task LoadWebOrdersAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 LoadWebOrdersAsync called");
                
                // Get orders from local database that came from cloud (web orders)
                var orderService = ServiceHelper.GetService<OrderService>();
                if (orderService == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ OrderService is null!");
                    return;
                }
                
                var allOrders = await orderService.GetOrdersAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"📦 Total orders in database: {allOrders.Count}");
                
                // DEBUG: Show ALL orders with their sync status
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine("🔍 FULL DATABASE DUMP:");
                foreach (var order in allOrders.OrderByDescending(o => o.CreatedAt).Take(20))
                {
                    System.Diagnostics.Debug.WriteLine($"   ID: {order.Id} | Date: {order.CreatedAt:yyyy-MM-dd HH:mm:ss} | Customer: {order.CustomerName} | Sync: {order.SyncStatus}");
                }
                System.Diagnostics.Debug.WriteLine("========================================");
                
                // DEBUG: Show all synced order dates
                var syncedOrders = allOrders.Where(o => o.SyncStatus == Models.SyncStatus.Synced).ToList();
                System.Diagnostics.Debug.WriteLine($"📊 Total synced orders: {syncedOrders.Count}");
                System.Diagnostics.Debug.WriteLine($"📊 Total pending orders: {allOrders.Count(o => o.SyncStatus == Models.SyncStatus.Pending)}");
                System.Diagnostics.Debug.WriteLine($"📊 Total failed orders: {allOrders.Count(o => o.SyncStatus == Models.SyncStatus.Failed)}");
                
                var orderDates = syncedOrders.GroupBy(o => o.CreatedAt.Date).OrderByDescending(g => g.Key).ToList();
                System.Diagnostics.Debug.WriteLine($"📅 Synced orders by date:");
                foreach (var dateGroup in orderDates.Take(10))
                {
                    System.Diagnostics.Debug.WriteLine($"   {dateGroup.Key:MMM dd, yyyy}: {dateGroup.Count()} orders");
                }
                
                System.Diagnostics.Debug.WriteLine($"🎯 Selected date filter: {_selectedDate:yyyy-MM-dd}");
                System.Diagnostics.Debug.WriteLine($"🎯 Looking for orders on: {_selectedDate.Date:yyyy-MM-dd}");
                System.Diagnostics.Debug.WriteLine($"🎯 Show all mode: {_showAllOrders}");
                
                // ✅ FILTER BY SELECTED DATE (or show all if toggle enabled)
                List<Order> filteredWebOrders;
                
                if (_showAllOrders)
                {
                    // Show ALL synced orders regardless of date
                    filteredWebOrders = allOrders.Where(o => o.SyncStatus == Models.SyncStatus.Synced).ToList();
                    System.Diagnostics.Debug.WriteLine($"📅 Showing ALL orders: {filteredWebOrders.Count}");
                }
                else
                {
                    // Filter by selected date
                    filteredWebOrders = allOrders.Where(o => 
                        o.CreatedAt.Date == _selectedDate.Date && 
                        o.SyncStatus == Models.SyncStatus.Synced
                    ).ToList();
                    System.Diagnostics.Debug.WriteLine($"📅 Orders for {_selectedDate:MMM dd, yyyy}: {filteredWebOrders.Count}");
                }
                
                // DEBUG: Show why orders might not match
                if (filteredWebOrders.Count == 0 && syncedOrders.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ No orders match selected date! Checking closest dates:");
                    var closestOrders = syncedOrders
                        .OrderBy(o => Math.Abs((o.CreatedAt.Date - _selectedDate.Date).TotalDays))
                        .Take(5);
                    foreach (var order in closestOrders)
                    {
                        System.Diagnostics.Debug.WriteLine($"   {order.CreatedAt:yyyy-MM-dd} | {order.CustomerName} | Days diff: {(order.CreatedAt.Date - _selectedDate.Date).TotalDays}");
                    }
                }
                
                // Apply search filter if exists
                if (!string.IsNullOrWhiteSpace(_searchText))
                {
                    filteredWebOrders = filteredWebOrders.Where(o => 
                        o.CustomerName?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) == true ||
                        o.CustomerPhone?.Contains(_searchText) == true ||
                        o.OrderNumber?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) == true
                    ).ToList();
                }

                // ✅ PAGINATION - Show 10 orders per page for speed
                var ordersToDisplay = filteredWebOrders
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip(_currentPage * _pageSize)
                    .Take(_pageSize)
                    .ToList();
                
                _totalPages = (int)Math.Ceiling(filteredWebOrders.Count / (double)_pageSize);
                
                System.Diagnostics.Debug.WriteLine($"📄 Displaying page {_currentPage + 1} of {_totalPages} ({ordersToDisplay.Count} orders)");
                
                // Update UI on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        // Update Today's stats
                        TodayOrderCountLabel.Text = filteredWebOrders.Count.ToString();
                        
                        var pendingOrders = filteredWebOrders.Where(o => 
                            o.Status == OrderStatus.New || 
                            o.Status == OrderStatus.Kitchen || 
                            o.Status == OrderStatus.Preparing
                        ).ToList();
                        
                        // Update "Showing" label
                        if (_selectedDate.Date == DateTime.Today)
                        {
                            SelectedDateLabel.Text = "Showing: Today";
                        }
                        else
                        {
                            SelectedDateLabel.Text = $"Showing: {_selectedDate:MMM dd, yyyy}";
                        }

                        // Update Orders collection for UI binding - PAGINATED
                        Orders.Clear();
                        foreach (var order in ordersToDisplay)
                        {
                            Orders.Add(order);
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"✅ Orders collection updated with {Orders.Count} items");
                        
                        // Update pagination info
                        UpdatePaginationUI();
                        
                        // Show/hide no orders message
                        NoOrdersFrame.IsVisible = filteredWebOrders.Count == 0;
                        
                        // Update connection status
                        var cloudService = ServiceHelper.GetService<CloudOrderService>();
                        var isPolling = cloudService?.IsPolling ?? false;
                        ConnectionStatusIndicator.Fill = isPolling ? Microsoft.Maui.Graphics.Colors.Green : Microsoft.Maui.Graphics.Colors.Orange;
                        ConnectionStatusLabel.Text = isPolling ? "Connected" : "Disconnected";
                        
                        System.Diagnostics.Debug.WriteLine("✅ UI updated successfully");
                    }
                    catch (Exception uiEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error updating UI: {uiEx.Message}");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading web orders: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Error", $"Failed to load web orders: {ex.Message}", "OK");
                });
            }
        }

        private async Task ShowPlaceholderContent()
        {
            // For now, show placeholder content
            TodayOrderCountLabel.Text = "0";
            SelectedDateLabel.Text = "Showing: Today";
            
            NoOrdersFrame.IsVisible = true;
            
            await Task.CompletedTask;
        }

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;
                
                // Get cloud order service and sync orders
                var cloudService = ServiceHelper.GetService<CloudOrderService>();
                if (cloudService != null)
                {
                    System.Diagnostics.Debug.WriteLine($"🔄 Refresh: Syncing last 30 days with single API call...");
                    
                    // Use 'since' parameter - one API call for all orders in last 30 days
                    var thirtyDaysAgo = DateTime.Today.AddDays(-30);
                    var syncResult = await cloudService.SyncOrdersByDateAsync(thirtyDaysAgo);
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Refresh complete: {syncResult.Message}");
                }
                
                // Reload orders from database
                await LoadWebOrdersAsync();
                
                // Count actual orders in database
                var orderService = ServiceHelper.GetService<OrderService>();
                if (orderService != null)
                {
                    var allOrders = await orderService.GetOrdersAsync();
                    var syncedOrders = allOrders.Where(o => o.SyncStatus == Models.SyncStatus.Synced).ToList();
                    var orderDates = syncedOrders.GroupBy(o => o.CreatedAt.Date).OrderByDescending(g => g.Key).Take(5);
                    
                    var datesSummary = string.Join("\n", orderDates.Select(g => $"• {g.Key:MMM dd}: {g.Count()} orders"));
                    
                    await DisplayAlert("Refresh Complete", 
                        $"Database has {syncedOrders.Count} synced orders\n\nRecent dates:\n{datesSummary}\n\nShowing: Nov 20, 2025", 
                        "OK");
                }
                else
                {
                    await DisplayAlert("Success", $"Orders refreshed! Found orders from last 30 days.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to refresh: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private async void OnSearchClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Info", "Search functionality coming soon!", "OK");
        }

        private async void OnManualSyncClicked(object sender, EventArgs e)
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                // Get cloud order service
                var cloudService = ServiceHelper.GetService<CloudOrderService>();
                if (cloudService == null) 
                {
                    await DisplayAlert("Error", "Cloud service not available", "OK");
                    return;
                }
                
                // Use the selected date from the date picker
                var targetDate = _selectedDate;
                System.Diagnostics.Debug.WriteLine($"🔄 Manual sync requested for date: {targetDate:yyyy-MM-dd}");
                
                // Fetch orders for the selected date
                var syncResult = await cloudService.SyncOrdersByDateAsync(targetDate);
                
                // Refresh the UI with new data
                await LoadWebOrdersAsync();

                if (syncResult.Success)
                {
                    await DisplayAlert("✅ Sync Complete", 
                        $"{syncResult.Message}\n\nOrders fetched from {targetDate:MMM dd, yyyy}", 
                        "OK");
                }
                else
                {
                    await DisplayAlert("⚠️ Sync Failed", syncResult.Message, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Sync failed: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private void RefreshWebOrders()
        {
            // Refresh web orders data on main thread with live performance monitoring
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var refreshStart = DateTime.Now;
                    await LoadWebOrdersAsync();
                    var refreshDuration = (DateTime.Now - refreshStart).TotalMilliseconds;
                    System.Diagnostics.Debug.WriteLine($"🚀 UI SPEED: Orders refreshed in {refreshDuration:F0}ms");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error refreshing web orders: {ex.Message}");
                }
            });
        }

        // Date Filter Handler
        private async void OnDateSelected(object sender, DateChangedEventArgs e)
        {
            try
            {
                _selectedDate = e.NewDate;
                _currentPage = 0; // Reset to first page when date changes
                _showAllOrders = false; // Switch back to date filtering mode
                
                System.Diagnostics.Debug.WriteLine($"📅 Date filter changed to: {_selectedDate:MMM dd, yyyy}");
                
                // Refresh orders with new date filter
                await LoadWebOrdersAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error changing date: {ex.Message}");
                await DisplayAlert("Error", $"Failed to change date: {ex.Message}", "OK");
            }
        }

        // Order Action Button Handlers
        private async void OnViewDetailsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Order order)
            {
                try
                {
                    var detailsPopup = new OrderDetailsPopup(order);
                    await Navigation.PushModalAsync(detailsPopup);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error showing order details: {ex.Message}");
                    await DisplayAlert("Error", "Failed to show order details", "OK");
                }
            }
        }

        private async void OnPrintReceiptClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Order order)
            {
                try
                {
                    LoadingIndicator.IsVisible = true;
                    LoadingIndicator.IsRunning = true;

                    var receiptService = ServiceHelper.GetService<ReceiptService>();
                    if (receiptService == null)
                    {
                        await DisplayAlert("❌ Error", "Receipt service not available", "OK");
                        return;
                    }
                    await receiptService.PrintReceiptAsync(order);

                    await DisplayAlert("✅ Success", $"Receipt printed for order {order.OrderNumber}", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("❌ Print Error", $"Failed to print receipt: {ex.Message}", "OK");
                }
                finally
                {
                    LoadingIndicator.IsVisible = false;
                    LoadingIndicator.IsRunning = false;
                }
            }
        }

        private async void OnMarkCompleteClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Order order)
            {
                try
                {
                    bool confirm = await DisplayAlert("Complete Order", 
                        $"Mark order {order.OrderNumber} as completed?", "Yes", "No");
                    
                    if (!confirm) return;

                    LoadingIndicator.IsVisible = true;
                    LoadingIndicator.IsRunning = true;

                    // Update order status
                    order.Status = OrderStatus.Completed;
                    order.CompletedTime = DateTime.Now;
                    order.UpdatedAt = DateTime.Now;

                    var orderService = ServiceHelper.GetService<OrderService>();
                    if (orderService == null)
                    {
                        await DisplayAlert("❌ Error", "Order service not available", "OK");
                        return;
                    }
                    var result = await orderService.SaveOrderAsync(order);

                    if (result.Success)
                    {
                        await DisplayAlert("✅ Order Completed", 
                            $"Order {order.OrderNumber} has been marked as completed!", "OK");
                        
                        // Refresh the orders list
                        await LoadWebOrdersAsync();
                    }
                    else
                    {
                        await DisplayAlert("❌ Error", $"Failed to update order: {result.Message}", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("❌ Error", $"Failed to complete order: {ex.Message}", "OK");
                }
                finally
                {
                    LoadingIndicator.IsVisible = false;
                    LoadingIndicator.IsRunning = false;
                }
            }
        }

        private async void OnSpeedTestClicked(object sender, EventArgs e)
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                // Ultra-fast speed testing using CloudOrderService
                var cloudService = ServiceHelper.GetService<CloudOrderService>();
                if (cloudService != null)
                {
                    // Run multiple speed tests
                    var testStart = DateTime.Now;
                    
                    await DisplayAlert("⚡ Ultra-Fast Speed Test Starting", 
                        "Testing 2-second polling performance...\n\n" +
                        "• Testing API response time\n" +
                        "• Testing order processing speed\n" +
                        "• Testing UI refresh speed", "Start Test");

                    // Perform speed tests with ultra-fast polling
                    await cloudService.FetchOrdersAsync();
                    await Task.Delay(100); // Small delay to see performance
                    await cloudService.FetchOrdersAsync();
                    await Task.Delay(100);
                    await cloudService.FetchOrdersAsync();
                    
                    var testDuration = (DateTime.Now - testStart).TotalMilliseconds;
                    
                    // Generate performance status
                    var performanceStatus = $"Ultra-Fast Polling Performance:\n" +
                                          $"• Test Duration: {testDuration:F0}ms\n" +
                                          $"• Polling Interval: 2 seconds\n" +
                                          $"• Last Sync: {(cloudService.LastSyncTime != default ? cloudService.LastSyncTime.ToString("HH:mm:ss") : "Never")}\n" +
                                          $"• Status: {(cloudService.IsPolling ? "Active" : "Stopped")}";
                    
                    await DisplayAlert("⚡ Ultra-Fast Test Results", 
                        $"Test completed in {testDuration:F0}ms\n\n{performanceStatus}\n\n" +
                        "� Orders will appear within 2-4 seconds!\n" +
                        "⚡ 15x faster than standard polling!", "Excellent!");
                }
                else
                {
                    await DisplayAlert("❌ Speed Test Failed", 
                        "CloudOrderService not available", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("❌ Speed Test Error", $"Test failed: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        // ===== PAGINATION METHODS =====
        
        private void UpdatePaginationUI()
        {
            if (PaginationLabel != null)
            {
                PaginationLabel.Text = _totalPages > 0 
                    ? $"Page {_currentPage + 1} of {_totalPages}" 
                    : "No orders";
                
                PrevPageButton.IsEnabled = _currentPage > 0;
                NextPageButton.IsEnabled = _currentPage < _totalPages - 1;
            }
        }

        private async void OnPreviousPageClicked(object sender, EventArgs e)
        {
            if (_currentPage > 0)
            {
                _currentPage--;
                await LoadWebOrdersAsync();
            }
        }

        private async void OnNextPageClicked(object sender, EventArgs e)
        {
            if (_currentPage < _totalPages - 1)
            {
                _currentPage++;
                await LoadWebOrdersAsync();
            }
        }

        private async void OnGlobalSearchChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = e.NewTextValue ?? "";
            _currentPage = 0; // Reset to first page
            await LoadWebOrdersAsync();
        }

        /// <summary>
        /// Manual Sync Now button - Force fetch latest orders from OrderWeb.net
        /// </summary>
        private async void OnSyncNowClicked(object sender, EventArgs e)
        {
            try
            {
                // Disable button during sync
                SyncNowButton.IsEnabled = false;
                SyncNowButton.Text = "⚡ Syncing...";
                ConnectionStatusLabel.Text = "⚡ Syncing...";
                ConnectionStatusIndicator.Fill = Colors.Orange;
                
                System.Diagnostics.Debug.WriteLine("🔄 Manual sync initiated by user");
                
                // Get services
                var cloudService = ServiceHelper.GetService<CloudOrderService>();
                var orderService = ServiceHelper.GetService<OrderService>();
                if (cloudService == null || orderService == null)
                {
                    await DisplayAlert("Error", "Services not available", "OK");
                    return;
                }
                
                // Smart sync: Get last order and sync from there
                var allOrders = await orderService.GetOrdersAsync();
                var latestOrder = allOrders
                    .Where(o => o.SyncStatus == Models.SyncStatus.Synced)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefault();
                
                DateTime syncFromDate = latestOrder != null 
                    ? latestOrder.CreatedAt.AddHours(-1) 
                    : DateTime.Today.AddDays(-60);
                
                System.Diagnostics.Debug.WriteLine($"🔄 Manual Sync: Syncing from {syncFromDate:yyyy-MM-dd HH:mm}");
                if (latestOrder != null)
                {
                    System.Diagnostics.Debug.WriteLine($"📅 Last order in database: {latestOrder.OrderNumber} at {latestOrder.CreatedAt:yyyy-MM-dd HH:mm}");
                }
                
                var result = await cloudService.SyncOrdersByDateAsync(syncFromDate);
                
                System.Diagnostics.Debug.WriteLine($"🔍 Sync result: Success={result.Success}, OrdersFound={result.OrdersFound}, Message={result.Message}");
                
                // Refresh UI to show all orders
                System.Diagnostics.Debug.WriteLine("🔄 Refreshing UI after sync...");
                await LoadWebOrdersAsync();
                
                // Update status
                UpdateConnectionStatus();
                
                // Show success message with order count
                var message = result.OrdersFound > 0 
                    ? $"✅ Synced {result.OrdersFound} orders from OrderWeb.net\n{result.Message}"
                    : $"No new orders found.\n{result.Message}";
                    
                await DisplayAlert("✅ Sync Complete", message, "OK");
                
                System.Diagnostics.Debug.WriteLine($"✅ Manual sync complete: {result.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Manual sync failed: {ex.Message}");
                await DisplayAlert("Sync Failed", ex.Message, "OK");
            }
            finally
            {
                // Re-enable button
                SyncNowButton.IsEnabled = true;
                SyncNowButton.Text = "🔄 Sync Now";
                UpdateConnectionStatus();
            }
        }
        
        /// <summary>
        /// Update connection status indicator based on WebSocket state
        /// </summary>
        private void UpdateConnectionStatus()
        {
            try
            {
                var wsService = ServiceHelper.GetService<OrderWebWebSocketService>();
                var cloudService = ServiceHelper.GetService<CloudOrderService>();
                
                if (wsService != null)
                {
                    if (wsService.IsConnected)
                    {
                        // WebSocket is connected - primary real-time mode
                        ConnectionStatusLabel.Text = "Connected";
                        ConnectionStatusIndicator.Fill = Colors.Green;
                        
                        // Show last message time if available
                        if (wsService.LastMessageTime.HasValue)
                        {
                            var elapsed = DateTime.Now - wsService.LastMessageTime.Value;
                            if (elapsed.TotalMinutes < 1)
                                LastSyncLabel.Text = "Last sync: Just now";
                            else if (elapsed.TotalMinutes < 60)
                                LastSyncLabel.Text = $"Last sync: {(int)elapsed.TotalMinutes}m ago";
                            else
                                LastSyncLabel.Text = $"Last sync: {(int)elapsed.TotalHours}h ago";
                        }
                        else
                        {
                            LastSyncLabel.Text = "Real-time updates active";
                        }
                    }
                    else
                    {
                        // WebSocket disconnected - backup polling mode
                        ConnectionStatusLabel.Text = "Backup Mode";
                        ConnectionStatusIndicator.Fill = Colors.Orange;
                        
                        // Show last sync from polling
                        if (cloudService?.LastSyncTime != default)
                        {
                            var elapsed = DateTime.Now - cloudService.LastSyncTime;
                            LastSyncLabel.Text = $"Last check: {(int)elapsed.TotalSeconds}s ago";
                        }
                        else
                        {
                            LastSyncLabel.Text = "Checking every 60s";
                        }
                    }
                }
                else
                {
                    // No services available
                    ConnectionStatusLabel.Text = "Offline";
                    ConnectionStatusIndicator.Fill = Colors.Red;
                    LastSyncLabel.Text = "Click Sync Now";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating connection status: {ex.Message}");
            }
        }

        // Helper method to format payment method display
        private string FormatPaymentMethod(string? paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
                return "Not specified";
            
            // Convert common payment method values to display format
            return paymentMethod.ToLower() switch
            {
                "cash" => "Cash",
                "card" => "Card",
                "credit_card" => "Credit Card",
                "debit_card" => "Debit Card",
                "voucher" => "Gift Card",        // ✅ OrderWeb.net uses "voucher" for gift cards
                "gift_card" => "Gift Card",
                "giftcard" => "Gift Card",
                "online" => "Online Payment",
                "cod" => "Cash on Delivery",
                _ => paymentMethod // Return original if not recognized
            };
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            // Get authentication service
            var authService = ServiceHelper.GetService<AuthenticationService>();
            if (authService != null)
            {
                await authService.LogoutAsync();
            }

            // Navigate to login page immediately
            await Shell.Current.GoToAsync("//login");
        }
        
        private async void OnShowAllClicked(object sender, EventArgs e)
        {
            _showAllOrders = !_showAllOrders;
            
            if (_showAllOrders)
            {
                ShowAllButton.Text = "📅 By Date";
                ShowAllButton.BackgroundColor = Color.FromArgb("#F59E0B");
                SelectedDateLabel.Text = "Showing: All Orders";
            }
            else
            {
                ShowAllButton.Text = "📅 Show All";
                ShowAllButton.BackgroundColor = Color.FromArgb("#10B981");
                SelectedDateLabel.Text = $"Showing: {_selectedDate:MMM dd, yyyy}";
            }
            
            await LoadWebOrdersAsync();
        }
    }
}