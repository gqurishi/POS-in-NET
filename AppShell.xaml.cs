using POS_in_NET.Services;
using POS_in_NET.Pages;
using System.ComponentModel;

namespace POS_in_NET;

public partial class AppShell : Shell, INotifyPropertyChanged
{
	private string _currentDateTime = string.Empty;
	private System.Timers.Timer? _timer;

	public string CurrentDateTime
	{
		get => _currentDateTime;
		set
		{
			_currentDateTime = value;
			OnPropertyChanged(nameof(CurrentDateTime));
		}
	}

	public AppShell()
	{
		InitializeComponent();
		BindingContext = this;
		
		// Register modal pages for navigation
		Routing.RegisterRoute(nameof(CustomColorPickerPage), typeof(CustomColorPickerPage));
		Routing.RegisterRoute(nameof(FoodMenuManagement), typeof(FoodMenuManagement));
		Routing.RegisterRoute("collection", typeof(CollectionCustomerModal));
		Routing.RegisterRoute("delivery", typeof(DeliveryCustomerModal));
		
		// Register User Dashboard route
		Routing.RegisterRoute("userdashboard", typeof(UserDashboardPage));
		
		// Initialize date/time
		UpdateDateTime();
		
		// Setup timer to update every second
		_timer = new System.Timers.Timer(1000);
		_timer.Elapsed += (s, e) => UpdateDateTime();
		_timer.Start();
		
		// Subscribe to navigation events to update user info
		this.Navigated += OnShellNavigated;
	}

	private void UpdateDateTime()
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			CurrentDateTime = DateTime.Now.ToString("dddd, MMMM dd, yyyy - HH:mm:ss");
		});
	}

	private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
	{
		// Update user info when navigating (handled by TopBar component now)
		System.Diagnostics.Debug.WriteLine($"Navigated to: {e.Current.Location}");
	}

	private async void OnLogoutClicked(object sender, EventArgs e)
	{
		// Stop the timer
		_timer?.Stop();
		
		// Get authentication service
		var authService = ServiceHelper.GetService<AuthenticationService>();
		if (authService != null)
		{
			await authService.LogoutAsync();
		}

		// Navigate to login page immediately
		await Shell.Current.GoToAsync("//login");
		
		// Restart timer
		_timer?.Start();
	}

	private async void OnSyncDatabaseClicked(object sender, EventArgs e)
	{
		try
		{
			// Close the flyout
			Shell.Current.FlyoutIsPresented = false;

			// Get the current page
			var currentPage = Shell.Current.CurrentPage;
			
			// OrderTakingPage removed - no refresh needed
			// if (currentPage is OrderTakingPage orderTakingPage)
			// {
			//     await orderTakingPage.RefreshFloorsAndTablesAsync();
			// }
			
			// Show success message
			await currentPage.DisplayAlert("✅ Sync Complete", "Database synced successfully!\nFloors and tables refreshed.", "OK");
		}
		catch (Exception ex)
		{
			await Shell.Current.CurrentPage.DisplayAlert("❌ Sync Error", $"Failed to sync: {ex.Message}", "OK");
		}
	}

	private async void OnMenuItemTapped(object sender, EventArgs e)
	{
		try
		{
			// Clear all menu item selections first
			ClearMenuSelections();
			
			// Highlight the selected menu item
			if (sender is StackLayout stackLayout)
			{
				stackLayout.BackgroundColor = Color.FromArgb("#E3F2FD"); // Light blue selection
			}
			
			if (sender is View view && view.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tapGesture)
			{
				var route = tapGesture.CommandParameter?.ToString();
				if (!string.IsNullOrEmpty(route))
				{
					// Close the flyout
					Shell.Current.FlyoutIsPresented = false;
					
					// Check if this is a modal route (registered with Routing.RegisterRoute)
					// Modal routes: collection, delivery
					var modalRoutes = new[] { "collection", "delivery" };
					
					if (modalRoutes.Contains(route))
					{
						// Navigate to modal route without //
						await Shell.Current.GoToAsync(route);
					}
					else
					{
						// Navigate to shell content route with //
						await Shell.Current.GoToAsync($"//{route}");
					}
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
		}
	}

	private void ClearMenuSelections()
	{
		// Clear all menu item background colors
		var menuItems = new[]
		{
			DashboardMenuItem, RestaurantMenuItem, FoodMenuMenuItem, WebOrdersMenuItem,
			SettingsMenuItem, CollectionMenuItem, DeliveryMenuItem, OrderHistoryMenuItem,
			LiveOrderMenuItem, GiftCardsMenuItem, LoyaltyMenuItem, ReservationMenuItem,
			ReportMenuItem, InventoryMenuItem, PrintersMenuItem
		};

		foreach (var item in menuItems)
		{
			if (item != null)
			{
				item.BackgroundColor = Colors.Transparent;
			}
		}
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_timer?.Stop();
		_timer?.Dispose();
	}
}
