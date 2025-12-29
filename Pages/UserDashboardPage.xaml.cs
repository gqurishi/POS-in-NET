using POS_in_NET.Services;
using POS_in_NET.Models;
using System.Timers;

namespace POS_in_NET.Pages;

public partial class UserDashboardPage : ContentPage
{
    private readonly AuthenticationService _authService;
    private System.Timers.Timer? _timeTimer;
    private User? _currentUser;

    public UserDashboardPage()
    {
        InitializeComponent();
        _authService = AuthenticationService.Instance;
        
        // Start time updates
        StartTimeUpdates();
        
        // Load current user info
        LoadCurrentUser();
    }

    private void StartTimeUpdates()
    {
        // Update time immediately
        UpdateTimeDisplay();
        
        // Update every second
        _timeTimer = new System.Timers.Timer(1000);
        _timeTimer.Elapsed += (sender, e) =>
        {
            MainThread.BeginInvokeOnMainThread(UpdateTimeDisplay);
        };
        _timeTimer.Start();
    }

    private void UpdateTimeDisplay()
    {
        try
        {
            var now = DateTime.Now;
            DateLabel.Text = now.ToString("dddd, MMMM dd, yyyy");
            TimeLabel.Text = now.ToString("HH:mm:ss");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Time update error: {ex.Message}");
        }
    }

    private void LoadCurrentUser()
    {
        try
        {
            _currentUser = _authService.GetCurrentUser();
            if (_currentUser != null)
            {
                WelcomeLabel.Text = $"Welcome, {_currentUser.Name}!";
                System.Diagnostics.Debug.WriteLine($"User dashboard loaded for: {_currentUser.Name}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load user error: {ex.Message}");
            WelcomeLabel.Text = "Welcome, User!";
        }
    }

    private async void OnCollectionClicked(object sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Collection button clicked - User Dashboard");
            
            // Navigate to collection page
            await Shell.Current.GoToAsync("//collection");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Collection navigation error: {ex.Message}");
            await DisplayAlert("Error", "Unable to open Collection module", "OK");
        }
    }

    private async void OnDeliveryClicked(object sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Delivery button clicked - User Dashboard");
            
            // Navigate to delivery page
            await Shell.Current.GoToAsync("//delivery");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Delivery navigation error: {ex.Message}");
            await DisplayAlert("Error", "Unable to open Delivery module", "OK");
        }
    }

    private async void OnRestaurantClicked(object sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Restaurant button clicked - User Dashboard - Navigating directly to visual tables");
            
            // Navigate directly to visual table layout (as requested)
            await Shell.Current.GoToAsync("//restaurant/visual");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Restaurant navigation error: {ex.Message}");
            
            // Fallback: Navigate to regular restaurant page
            try
            {
                await Shell.Current.GoToAsync("//restaurant");
            }
            catch (Exception fallbackEx)
            {
                System.Diagnostics.Debug.WriteLine($"Restaurant fallback navigation error: {fallbackEx.Message}");
                await DisplayAlert("Error", "Unable to open Restaurant module", "OK");
            }
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        try
        {
            bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
            
            if (confirm)
            {
                System.Diagnostics.Debug.WriteLine("User logging out from User Dashboard");
                
                // Stop timer
                _timeTimer?.Stop();
                _timeTimer?.Dispose();
                
                // Logout user
                await _authService.LogoutAsync();
                
                // Navigate back to login
                await Shell.Current.GoToAsync("//login");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
            await DisplayAlert("Error", "Unable to logout properly", "OK");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Clean up timer
        _timeTimer?.Stop();
        _timeTimer?.Dispose();
    }
}