using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using POS_in_NET.Services;

namespace POS_in_NET.Pages
{
    public partial class RestaurantPage : ContentPage
    {
        private readonly FloorService _floorService;
        private readonly RestaurantTableService _tableService;
        private Timer? _statusUpdateTimer;

        public RestaurantPage()
        {
            InitializeComponent();
            
            // Set the page title in the TopBar
            TopBar.SetPageTitle("Restaurant Overview");
            
            _floorService = new FloorService();
            _tableService = new RestaurantTableService();

            // Start status update timer (every 10 seconds)
            _statusUpdateTimer = new Timer(_ => 
            {
                MainThread.BeginInvokeOnMainThread(() => UpdateConnectionStatus());
            }, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadRestaurantStatsAsync();
            UpdateConnectionStatus();
        }

        private async Task LoadRestaurantStatsAsync()
        {
            try
            {
                // Stats removed from new design - can be added back if needed
                System.Diagnostics.Debug.WriteLine($"✅ Restaurant Overview page loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading restaurant page: {ex.Message}");
            }
        }

        private async void OnFloorManagementClicked(object sender, EventArgs e)
        {
            try
            {
                // Navigate to Floor Management page
                await Shell.Current.GoToAsync("//floor");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Navigation Error", 
                    $"Could not navigate to Floor Management: {ex.Message}", 
                    "OK");
            }
        }

        private async void OnTableManagementClicked(object sender, EventArgs e)
        {
            try
            {
                // Navigate to Table Management page
                await Shell.Current.GoToAsync("//table");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Navigation Error", 
                    $"Could not navigate to Table Management: {ex.Message}", 
                    "OK");
            }
        }

        private async void OnVisualLayoutClicked(object sender, EventArgs e)
        {
            try
            {
                // Navigate to Visual Table Layout page
                await Shell.Current.GoToAsync("//visual");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Navigation Error", 
                    $"Could not navigate to Visual Layout: {ex.Message}", 
                    "OK");
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Logout error: {ex.Message}");
                // Still navigate to login even if logout service fails
                await Shell.Current.GoToAsync("//login");
            }
        }

        /// <summary>
        /// Refresh button - Reload restaurant data
        /// </summary>
        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Refreshing restaurant data...");
                await LoadRestaurantStatsAsync();
                UpdateConnectionStatus();
                System.Diagnostics.Debug.WriteLine("✅ Restaurant data refreshed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Refresh failed: {ex.Message}");
                await DisplayAlert("Refresh Failed", ex.Message, "OK");
            }
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
                
                System.Diagnostics.Debug.WriteLine("🔄 Manual sync initiated by user from Restaurant page");
                
                // Get cloud service
                var cloudService = ServiceHelper.GetService<CloudOrderService>();
                if (cloudService == null)
                {
                    await DisplayAlert("Error", "Cloud service not available", "OK");
                    return;
                }
                
                // Force fetch orders
                var result = await cloudService.FetchOrdersAsync();
                
                // SIMPLE FIX: Manually set correct payment methods for today's orders
                var paymentFix = ServiceHelper.GetService<PaymentFixService>();
                if (paymentFix != null)
                {
                    System.Diagnostics.Debug.WriteLine("🔧 Running manual payment fix...");
                    var fixResult = await paymentFix.FixTodaysPaymentsManuallyAsync();
                    System.Diagnostics.Debug.WriteLine($"✅ Payment fix result: {fixResult.Message}");
                }
                
                // Update status
                UpdateConnectionStatus();
                
                // Show success message briefly
                await DisplayAlert("✅ Sync Complete", 
                    result.Message + $"\nTotal orders today: {result.TotalOrders}", 
                    "OK");
                
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
                SyncNowButton.Text = "Sync Now";
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
    }
}
