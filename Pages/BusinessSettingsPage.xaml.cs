using POS_in_NET.Models;
using POS_in_NET.Services;

namespace POS_in_NET.Pages;

public partial class BusinessSettingsPage : ContentPage
{
    private readonly BusinessSettingsService _businessService;
    private readonly AuthenticationService _authService;
    private BusinessInfo? _currentBusinessInfo;

    public BusinessSettingsPage()
    {
        InitializeComponent();
        TopBar.SetPageTitle("Settings");
        _businessService = new BusinessSettingsService();
        _authService = AuthenticationService.Instance;
        
        // Update preview as user types
        RestaurantNameEntry.TextChanged += OnRestaurantNameChanged;
        DescriptionEntry.TextChanged += OnDescriptionChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Check if user is admin
        if (_authService.CurrentUser?.Role != UserRole.Admin)
        {
            await DisplayAlert("Access Denied", "Only administrators can access business settings.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        await LoadBusinessInfoAsync();
    }

    private async Task LoadBusinessInfoAsync()
    {
        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            _currentBusinessInfo = await _businessService.GetBusinessInfoAsync();

            if (_currentBusinessInfo != null)
            {
                RestaurantNameEntry.Text = _currentBusinessInfo.RestaurantName;
                DescriptionEntry.Text = _currentBusinessInfo.Description;
                AddressEditor.Text = _currentBusinessInfo.Address;
                PhoneEntry.Text = _currentBusinessInfo.PhoneNumber;
                EmailEntry.Text = _currentBusinessInfo.Email;
                TaxCodeEntry.Text = _currentBusinessInfo.TaxCode;
                
                // Load label printer settings
                LabelPrinterEnabledSwitch.IsToggled = _currentBusinessInfo.LabelPrinterEnabled;
                LabelPrinterIpEntry.Text = _currentBusinessInfo.LabelPrinterIp;
                LabelPrinterPortEntry.Text = _currentBusinessInfo.LabelPrinterPort.ToString();
                LabelPrinterSection.IsVisible = _currentBusinessInfo.LabelPrinterEnabled;

                UpdatePreview();
            }
            else
            {
                // Create default business info
                _currentBusinessInfo = new BusinessInfo
                {
                    Id = 1,
                    RestaurantName = "Restaurant POS",
                    Description = "Premium Dining Experience"
                };
                UpdatePreview();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load business information: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private void OnRestaurantNameChanged(object? sender, TextChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void OnDescriptionChanged(object? sender, TextChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        PreviewRestaurantName.Text = string.IsNullOrWhiteSpace(RestaurantNameEntry.Text) 
            ? "Restaurant Name" 
            : RestaurantNameEntry.Text;
        
        PreviewDescription.Text = string.IsNullOrWhiteSpace(DescriptionEntry.Text) 
            ? "Description" 
            : DescriptionEntry.Text;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(RestaurantNameEntry.Text))
            {
                await DisplayAlert("Validation Error", "Restaurant name is required.", "OK");
                RestaurantNameEntry.Focus();
                return;
            }

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            SaveButton.IsEnabled = false;

            // Update business info object
            if (_currentBusinessInfo == null)
            {
                _currentBusinessInfo = new BusinessInfo { Id = 1 };
            }

            _currentBusinessInfo.RestaurantName = RestaurantNameEntry.Text.Trim();
            _currentBusinessInfo.Description = DescriptionEntry.Text?.Trim() ?? "";
            _currentBusinessInfo.Address = AddressEditor.Text?.Trim() ?? "";
            _currentBusinessInfo.PhoneNumber = PhoneEntry.Text?.Trim() ?? "";
            _currentBusinessInfo.Email = EmailEntry.Text?.Trim() ?? "";
            _currentBusinessInfo.TaxCode = TaxCodeEntry.Text?.Trim() ?? "";
            
            // Save label printer settings
            _currentBusinessInfo.LabelPrinterEnabled = LabelPrinterEnabledSwitch.IsToggled;
            _currentBusinessInfo.LabelPrinterIp = LabelPrinterIpEntry.Text?.Trim();
            if (int.TryParse(LabelPrinterPortEntry.Text, out int port))
            {
                _currentBusinessInfo.LabelPrinterPort = port;
            }

            bool success;
            if (_currentBusinessInfo.Id > 0)
            {
                success = await _businessService.UpdateBusinessInfoAsync(_currentBusinessInfo, _authService.CurrentUser!.Username);
            }
            else
            {
                success = await _businessService.CreateBusinessInfoAsync(_currentBusinessInfo, _authService.CurrentUser!.Username);
            }

            if (success)
            {
                ShowStatusMessage("✅ Business information saved successfully!", Colors.Green);
                UpdatePreview();
                
                // Auto-hide status message after 3 seconds
                await Task.Delay(3000);
                HideStatusMessage();
            }
            else
            {
                ShowStatusMessage("❌ Failed to save business information.", Colors.Red);
            }
        }
        catch (Exception ex)
        {
            ShowStatusMessage($"❌ Error: {ex.Message}", Colors.Red);
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            SaveButton.IsEnabled = true;
        }
    }

    private async void OnResetClicked(object sender, EventArgs e)
    {
        var result = await DisplayAlert("Reset Form", "Are you sure you want to reset all changes?", "Yes", "No");
        if (result)
        {
            await LoadBusinessInfoAsync();
            HideStatusMessage();
        }
    }

    // Tab Navigation Handlers - Easy navigation between settings tabs
    private async void OnBusinessTabClicked(object? sender, EventArgs e)
    {
        // Already on this page, just highlight the tab
        UpdateTabHighlight("business");
    }

    private async void OnUserTabClicked(object? sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//usermanagement");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
        }
    }

    private async void OnCloudTabClicked(object? sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//cloudsettings");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
        }
    }

    private async void OnPostcodeTabClicked(object? sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//postcodelookup");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
        }
    }

    private void UpdateTabHighlight(string activeTab)
    {
        // Reset all tabs to default styling
        ResetTabToDefault(BusinessTabBorder);
        ResetTabToDefault(UserTabBorder);
        ResetTabToDefault(CloudTabBorder);
        ResetTabToDefault(PostcodeTabBorder);

        // Set all label colors to default
        SetTabLabelColor(BusinessTabBorder, "#6B7280");
        SetTabLabelColor(UserTabBorder, "#6B7280");
        SetTabLabelColor(CloudTabBorder, "#6B7280");
        SetTabLabelColor(PostcodeTabBorder, "#6B7280");

        // Highlight active tab with elegant styling
        switch (activeTab.ToLower())
        {
            case "business":
                HighlightActiveTab(BusinessTabBorder, "#E0F2FE", "#0EA5E9");
                break;
            case "user":
                HighlightActiveTab(UserTabBorder, "#DCFCE7", "#10B981");
                break;
            case "cloud":
                HighlightActiveTab(CloudTabBorder, "#F3E8FF", "#8B5CF6");
                break;
            case "postcode":
                HighlightActiveTab(PostcodeTabBorder, "#FEF2F2", "#EF4444");
                break;
        }
    }
    
    private void ResetTabToDefault(Border tabBorder)
    {
        tabBorder.BackgroundColor = Colors.White;
        tabBorder.Stroke = Color.FromArgb("#E5E7EB");
        tabBorder.StrokeThickness = 1;
    }
    
    private void HighlightActiveTab(Border tabBorder, string backgroundColor, string borderColor)
    {
        tabBorder.BackgroundColor = Color.FromArgb(backgroundColor);
        tabBorder.Stroke = Color.FromArgb(borderColor);
        tabBorder.StrokeThickness = 2;
        SetTabLabelColor(tabBorder, borderColor);
    }
    
    private void SetTabLabelColor(Border tabBorder, string color)
    {
        if (tabBorder.Content is Label label)
        {
            label.TextColor = Color.FromArgb(color);
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        // Navigate back to dashboard (where Business Settings is accessed from)
        await Shell.Current.GoToAsync("//dashboard");
    }

    private void ShowStatusMessage(string message, Color color)
    {
        StatusLabel.Text = message;
        StatusLabel.TextColor = color;
        StatusLabel.IsVisible = true;
    }

    private void HideStatusMessage()
    {
        StatusLabel.IsVisible = false;
    }

    private async void OnCloudSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//cloudsettings");
    }

    private async void OnUserManagementClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//usermanagement");
    }
    
    private void OnLabelPrinterToggled(object sender, ToggledEventArgs e)
    {
        LabelPrinterSection.IsVisible = e.Value;
    }
    
    private async void OnTestPrinterClicked(object sender, EventArgs e)
    {
        try
        {
            var ip = LabelPrinterIpEntry.Text?.Trim();
            if (string.IsNullOrEmpty(ip))
            {
                await DisplayAlert("Validation Error", "Please enter printer IP address first.", "OK");
                return;
            }
            
            if (!int.TryParse(LabelPrinterPortEntry.Text, out int port))
            {
                port = 9100;
            }
            
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            
            var labelService = new LabelPrintingService(ip, port, true);
            
            ShowStatusMessage("🔄 Testing printer connection...", Colors.Blue);
            
            var connected = await labelService.TestPrinterConnectionAsync();
            
            if (connected)
            {
                ShowStatusMessage("🖨️ Printing test label...", Colors.Blue);
                var printed = await labelService.PrintTestLabelAsync();
                
                if (printed)
                {
                    ShowStatusMessage("✅ Printer test successful! Check for test label.", Colors.Green);
                }
                else
                {
                    ShowStatusMessage("⚠️ Connected but failed to print. Check printer status.", Colors.Orange);
                }
            }
            else
            {
                ShowStatusMessage($"❌ Cannot connect to printer at {ip}:{port}", Colors.Red);
            }
            
            await Task.Delay(5000);
            HideStatusMessage();
        }
        catch (Exception ex)
        {
            ShowStatusMessage($"❌ Test failed: {ex.Message}", Colors.Red);
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private async void OnManagePrintGroupsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new PrintGroupsPage());
    }
}
