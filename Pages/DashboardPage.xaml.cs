using Microsoft.Maui.Controls;
using POS_in_NET.Services;
using POS_in_NET.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace POS_in_NET.Pages
{
    public partial class DashboardPage : ContentPage
    {
        private readonly AuthenticationService _authService;
        private readonly OrderService _orderService;
        private readonly CloudOrderService? _cloudService;

        public DashboardPage()
        {
            InitializeComponent();
            _authService = AuthenticationService.Instance;
            _orderService = new OrderService();
            _cloudService = ServiceHelper.GetService<CloudOrderService>();
            
            // Set the page title in the TopBar
            TopBar.SetPageTitle("Dashboard");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            System.Diagnostics.Debug.WriteLine("🏠 Dashboard appearing - starting initialization");
            
            try
            {
                LoadWelcomeMessage();
                System.Diagnostics.Debug.WriteLine("✅ Welcome message loaded");
                
                // Load dashboard data - run in background but catch errors
                _ = LoadDashboardDataSafely();
                
                System.Diagnostics.Debug.WriteLine("✅ Dashboard initialization complete");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Dashboard OnAppearing error: {ex.Message}");
            }
        }
        
        private async Task LoadDashboardDataSafely()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📊 Loading dashboard data...");
                await LoadDashboardData();
                System.Diagnostics.Debug.WriteLine("✅ Dashboard data loaded successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Dashboard data load failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        private void LoadWelcomeMessage()
        {
            try
            {
                var currentUser = _authService.GetCurrentUser();
                if (currentUser != null)
                {
                    WelcomeLabel.Text = $"Welcome, {currentUser.Username}!";
                }
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () => 
                    await DisplayAlert("Error", $"Failed to load user information: {ex.Message}", "OK"));
            }
        }

        private async Task LoadDashboardData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📊 Starting parallel data load...");
                
                // Load stats and cloud status in parallel with individual error handling
                var tasks = new List<Task>
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            await LoadTodaysStats();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ LoadTodaysStats error: {ex.Message}");
                        }
                    }),
                    Task.Run(async () =>
                    {
                        try
                        {
                            await LoadWeeklySales();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ LoadWeeklySales error: {ex.Message}");
                        }
                    }),
                    Task.Run(async () =>
                    {
                        try
                        {
                            await LoadMonthlySales();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ LoadMonthlySales error: {ex.Message}");
                        }
                    })
                };
                
                // Wait for all with timeout
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                var completedTask = await Task.WhenAny(Task.WhenAll(tasks), timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Dashboard data load timed out after 10 seconds");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("✅ Dashboard data loaded successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Dashboard load error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            }
        }

        private async Task LoadTodaysStats()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📈 Loading today's stats...");
                
                // Set defaults first on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (TodaysOrdersLabel != null)
                        TodaysOrdersLabel.Text = "0";
                    if (TodaysSalesLabel != null)
                        TodaysSalesLabel.Text = "£0.00";
                });

                // Try to load from database
                System.Diagnostics.Debug.WriteLine("📊 Querying database for orders...");
                var todayCompletedCount = await _orderService.GetTodayCompletedOrdersCountAsync();
                System.Diagnostics.Debug.WriteLine($"✅ Today's completed orders: {todayCompletedCount}");
                
                var allOrders = await _orderService.GetOrdersAsync();
                System.Diagnostics.Debug.WriteLine($"✅ Total orders retrieved: {allOrders.Count}");
                
                var todayOrders = allOrders.Where(o => o.CreatedAt.Date == DateTime.Today).ToList();
                System.Diagnostics.Debug.WriteLine($"✅ Today's orders filtered: {todayOrders.Count}");
                
                decimal todaysSales = 0;
                foreach (var order in todayOrders)
                {
                    todaysSales += order.TotalAmount;
                }
                System.Diagnostics.Debug.WriteLine($"✅ Today's sales calculated: £{todaysSales:F2}");

                // Update UI on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (TodaysOrdersLabel != null)
                        TodaysOrdersLabel.Text = todayCompletedCount.ToString();
                    if (TodaysSalesLabel != null)
                        TodaysSalesLabel.Text = $"£{todaysSales:F2}";
                });
                
                System.Diagnostics.Debug.WriteLine("✅ Today's stats updated in UI");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading today's stats: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (TodaysOrdersLabel != null)
                        TodaysOrdersLabel.Text = "0";
                    if (TodaysSalesLabel != null)
                        TodaysSalesLabel.Text = "£0.00";
                });
            }
        }

        private async Task LoadWeeklySales()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📈 Loading weekly sales...");
                
                // Set default first
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (WeeklySalesLabel != null)
                        WeeklySalesLabel.Text = "£0.00";
                });

                // Get start of week (Monday)
                var today = DateTime.Today;
                int daysToSubtract = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                var startOfWeek = today.AddDays(-daysToSubtract);

                var allOrders = await _orderService.GetOrdersAsync();
                var weekOrders = allOrders.Where(o => o.CreatedAt.Date >= startOfWeek && o.CreatedAt.Date <= today).ToList();
                
                decimal weeklySales = 0;
                foreach (var order in weekOrders)
                {
                    weeklySales += order.TotalAmount;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (WeeklySalesLabel != null)
                        WeeklySalesLabel.Text = $"£{weeklySales:F2}";
                });
                
                System.Diagnostics.Debug.WriteLine($"✅ Weekly sales: £{weeklySales:F2}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading weekly sales: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (WeeklySalesLabel != null)
                        WeeklySalesLabel.Text = "£0.00";
                });
            }
        }

        private async Task LoadMonthlySales()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📈 Loading monthly sales...");
                
                // Set default first
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (MonthlySalesLabel != null)
                        MonthlySalesLabel.Text = "£0.00";
                });

                // Get start of month
                var today = DateTime.Today;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);

                var allOrders = await _orderService.GetOrdersAsync();
                var monthOrders = allOrders.Where(o => o.CreatedAt.Date >= startOfMonth && o.CreatedAt.Date <= today).ToList();
                
                decimal monthlySales = 0;
                foreach (var order in monthOrders)
                {
                    monthlySales += order.TotalAmount;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (MonthlySalesLabel != null)
                        MonthlySalesLabel.Text = $"£{monthlySales:F2}";
                });
                
                System.Diagnostics.Debug.WriteLine($"✅ Monthly sales: £{monthlySales:F2}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading monthly sales: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (MonthlySalesLabel != null)
                        MonthlySalesLabel.Text = "£0.00";
                });
            }
        }

        private async void OnCollectionClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("collection");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to open collection page: {ex.Message}", "OK");
            }
        }

        private async void OnDeliveryClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("delivery");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to open delivery page: {ex.Message}", "OK");
            }
        }

        private async void OnRestaurantClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("//visuallayout");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to open restaurant layout: {ex.Message}", "OK");
            }
        }

        private async void OnTodaysSalesClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("//orderhistory");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to open order history: {ex.Message}", "OK");
            }
        }

        private async void OnViewWebOrdersClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("//weborders");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to navigate to web orders: {ex.Message}", "OK");
            }
        }

        private async void OnCloudSettingsClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("//cloudsettings");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to navigate to cloud settings: {ex.Message}", "OK");
            }
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
    }
}
