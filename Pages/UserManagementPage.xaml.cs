using Microsoft.Maui.Controls;
using POS_in_NET.Models;
using POS_in_NET.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace POS_in_NET.Pages;

public partial class UserManagementPage : ContentPage, INotifyPropertyChanged
{
    private readonly AuthenticationService _authService;
    private readonly OrderNumberService _orderNumberService;
    private ObservableCollection<UserViewModel> _users;
    private bool _isLoading;
    private string _selectedRoleText = "User";
    private int _selectedRoleIndex = 0;

    public UserManagementPage()
    {
        InitializeComponent();
        TopBar.SetPageTitle("Settings");
        _authService = AuthenticationService.Instance;
        _orderNumberService = new OrderNumberService(new DatabaseService());
        _users = new ObservableCollection<UserViewModel>();
        
        BindingContext = this;
        UsersCollectionView.ItemsSource = _users;
        
        // Initialize role picker programmatically
        SetupRolePicker();
    }
    
    private void SetupRolePicker()
    {
        // Clear any existing items
        RolePicker.Items.Clear();
        
        // Add role options
        RolePicker.Items.Add("User");
        RolePicker.Items.Add("Manager");
        RolePicker.Items.Add("Admin");
        
        // Set default selection
        RolePicker.SelectedIndex = 0;
        
        // Setup button fallback
        RoleButton.Text = "User";
        _selectedRoleText = "User";
        _selectedRoleIndex = 0;
        
        System.Diagnostics.Debug.WriteLine($"RolePicker setup complete. Items: {RolePicker.Items.Count}");
        
        // For now, let's use the button approach which is more reliable
        RolePicker.IsVisible = false;
        RoleButton.IsVisible = true;
    }

    public ObservableCollection<UserViewModel> Users
    {
        get => _users;
        set
        {
            _users = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
            LoadingIndicator.IsVisible = value;
            LoadingIndicator.IsRunning = value;
        }
    }

    public ICommand DeleteUserCommand => new Command<UserViewModel>(async (user) => await DeleteUserAsync(user));
    public ICommand EditUserCommand => new Command<UserViewModel>(async (user) => await EditUserAsync(user));

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Check if current user has admin privileges
        var currentUser = _authService.CurrentUser;
        if (currentUser?.Role != UserRole.Admin)
        {
            await ToastNotification.ShowAsync("Access Denied", "You don't have permission to access user management.", NotificationType.Error, 4000);
            await Shell.Current.GoToAsync("..");
            return;
        }
        
        // Ensure picker is properly initialized
        InitializePicker();
        
        await LoadUsersAsync();
    }
    
    private void InitializePicker()
    {
        // Ensure picker has items (in case setup didn't work)
        if (RolePicker.Items.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("Re-setting up picker items");
            SetupRolePicker();
        }
        
        // Debug: Log picker items
        System.Diagnostics.Debug.WriteLine($"Picker items count: {RolePicker.Items.Count}");
        for (int i = 0; i < RolePicker.Items.Count; i++)
        {
            System.Diagnostics.Debug.WriteLine($"Item {i}: {RolePicker.Items[i]}");
        }
        
        // Ensure default selection
        if (RolePicker.SelectedIndex == -1 && RolePicker.Items.Count > 0)
        {
            RolePicker.SelectedIndex = 0;
        }
        
        System.Diagnostics.Debug.WriteLine($"Selected index: {RolePicker.SelectedIndex}");
        if (RolePicker.SelectedIndex >= 0 && RolePicker.SelectedIndex < RolePicker.Items.Count)
        {
            System.Diagnostics.Debug.WriteLine($"Selected item: {RolePicker.Items[RolePicker.SelectedIndex]}");
        }
    }

    private async void OnCreateUserClicked(object sender, EventArgs e)
    {
        try
        {
            // Reset status
            StatusLabel.IsVisible = false;
            CreateUserButton.IsEnabled = false;

            // Validate inputs
            var name = NameEntry.Text?.Trim();
            var pin = PINEntry.Text?.Trim();
            
            // Get selected role index (use button selection if button is visible, otherwise picker)
            var selectedRoleIndex = RoleButton.IsVisible ? _selectedRoleIndex : RolePicker.SelectedIndex;

            System.Diagnostics.Debug.WriteLine($"Create user - Selected role index: {selectedRoleIndex}");
            System.Diagnostics.Debug.WriteLine($"Using button: {RoleButton.IsVisible}, Button text: {RoleButton.Text}");
            
            if (selectedRoleIndex >= 0)
            {
                var roleText = RoleButton.IsVisible ? _selectedRoleText : 
                              (RolePicker.SelectedIndex >= 0 && RolePicker.SelectedIndex < RolePicker.Items.Count ? 
                               RolePicker.Items[RolePicker.SelectedIndex] : "Unknown");
                System.Diagnostics.Debug.WriteLine($"Selected role text: {roleText}");
            }

            if (string.IsNullOrEmpty(name))
            {
                ShowStatus("Please enter the employee's name.", true);
                return;
            }

            if (string.IsNullOrEmpty(pin))
            {
                ShowStatus("Please enter a 4-digit PIN.", true);
                return;
            }

            if (pin.Length != 4 || !pin.All(char.IsDigit))
            {
                ShowStatus("PIN must be exactly 4 digits (0-9).", true);
                return;
            }

            if (selectedRoleIndex == -1)
            {
                ShowStatus("Please select a role.", true);
                return;
            }

            // Parse role - convert from picker index to UserRole enum
            UserRole role = (UserRole)selectedRoleIndex;
            System.Diagnostics.Debug.WriteLine($"Converted to UserRole: {role}");

            // Create user with PIN (use PIN as both username and password)
            var result = await _authService.CreateUserAsync(name, pin, pin, role);

            if (result.Success)
            {
                ShowStatus($"User '{name}' created successfully with PIN: {pin}", false);
                ClearForm();
                await LoadUsersAsync();
            }
            else
            {
                ShowStatus(result.Message, true);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Error creating user: {ex.Message}", true);
        }
        finally
        {
            CreateUserButton.IsEnabled = true;
        }
    }

    private async void OnRefreshUsersClicked(object sender, EventArgs e)
    {
        await LoadUsersAsync();
    }

    private async void OnRoleButtonClicked(object sender, EventArgs e)
    {
        try
        {
            var action = await DisplayActionSheet("Select Role", "Cancel", null, "User", "Manager", "Admin");
            
            if (action != null && action != "Cancel")
            {
                _selectedRoleText = action;
                RoleButton.Text = action;
                
                // Set corresponding index for compatibility
                _selectedRoleIndex = action switch
                {
                    "User" => 0,
                    "Manager" => 1,
                    "Admin" => 2,
                    _ => 0
                };
                
                System.Diagnostics.Debug.WriteLine($"Role selected via button: {action} (index: {_selectedRoleIndex})");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Role selection error: {ex.Message}");
        }
    }

    // Tab Navigation Handlers - Easy navigation between settings tabs
    private async void OnBusinessTabClicked(object? sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//businesssettings");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
        }
    }

    private async void OnUserTabClicked(object? sender, EventArgs e)
    {
        // Show User content, hide Order Number content
        OrderNumberContent.IsVisible = false;
        if (UsersCollectionView.Parent is VisualElement parentElement)
        {
            parentElement.IsVisible = true;
        }
        
        // Update tab highlight
        UpdateTabHighlight("user");
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
            // Navigate to Postcode Lookup page  
            var postcodeLookupService = ServiceHelper.GetService<PostcodeLookupService>();
            if (postcodeLookupService != null)
            {
                var postcodePage = new PostcodeLookupPage(postcodeLookupService);
                await Navigation.PushAsync(postcodePage);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
        }
    }

    private void OnOrderNumberTabClicked(object? sender, EventArgs e)
    {
        // Show Order Number content, hide User content
        OrderNumberContent.IsVisible = true;
        if (UsersCollectionView.Parent is VisualElement parentElement)
        {
            parentElement.IsVisible = false;
        }
        
        // Update tab highlight
        UpdateTabHighlight("ordernumber");
        
        // Load order number settings
        LoadOrderNumberSettings();
    }

    private void UpdateTabHighlight(string activeTab)
    {
        // Reset all tabs to default styling
        ResetTabToDefault(BusinessTabBorder);
        ResetTabToDefault(UserTabBorder);
        ResetTabToDefault(CloudTabBorder);
        ResetTabToDefault(OrderNumberTabBorder);
        ResetTabToDefault(PostcodeTabBorder);

        // Set all label colors to default
        SetTabLabelColor(BusinessTabBorder, "#6B7280");
        SetTabLabelColor(UserTabBorder, "#6B7280");
        SetTabLabelColor(CloudTabBorder, "#6B7280");
        SetTabLabelColor(OrderNumberTabBorder, "#6B7280");
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
            case "ordernumber":
                HighlightActiveTab(OrderNumberTabBorder, "#FEF2F2", "#EF4444");
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
        System.Diagnostics.Debug.WriteLine("Back button clicked");
        try
        {
            // Try multiple navigation approaches
            if (Shell.Current.Navigation.NavigationStack.Count > 1)
            {
                await Shell.Current.Navigation.PopAsync();
            }
            else
            {
                await Shell.Current.GoToAsync("//dashboard");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            // Fallback navigation
            await Shell.Current.GoToAsync("//dashboard");
        }
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            IsLoading = true;
            var users = await _authService.GetAllUsersAsync();
            var currentUserId = _authService.CurrentUser?.Id;

            _users.Clear();
            foreach (var user in users)
            {
                _users.Add(new UserViewModel
                {
                    Id = user.Id,
                    Name = user.Name,
                    Username = user.Username,
                    Role = user.Role.ToString(),
                    RoleColor = GetRoleColor(user.Role),
                    CreatedAt = user.CreatedAt,
                    CanDelete = user.Id != currentUserId // Can't delete self
                });
            }

            EmptyStateView.IsVisible = _users.Count == 0;
        }
        catch (Exception ex)
        {
            await ToastNotification.ShowAsync("Error", $"Failed to load users: {ex.Message}", NotificationType.Error, 4000);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteUserAsync(UserViewModel userViewModel)
    {
        try
        {
            var currentUser = _authService.CurrentUser;
            if (currentUser?.Id == userViewModel.Id)
            {
                await ToastNotification.ShowAsync("Error", "You cannot delete your own account.", NotificationType.Error, 4000);
                return;
            }

            var confirm = await DisplayAlert("Confirm Delete", 
                $"Are you sure you want to delete user '{userViewModel.Username}'?", 
                "Delete", "Cancel");

            if (!confirm) return;

            var result = await _authService.DeleteUserAsync(userViewModel.Id);
            if (result.Success)
            {
                await LoadUsersAsync();
                await ToastNotification.ShowAsync("Success", "User deleted successfully.", NotificationType.Success, 2000);
            }
            else
            {
                await ToastNotification.ShowAsync("Error", result.Message, NotificationType.Error, 4000);
            }
        }
        catch (Exception ex)
        {
            await ToastNotification.ShowAsync("Error", $"Failed to delete user: {ex.Message}", NotificationType.Error, 4000);
        }
    }

    private async Task EditUserAsync(UserViewModel userViewModel)
    {
        try
        {
            // Pre-fill the form with user data
            NameEntry.Text = userViewModel.Name;
            PINEntry.Text = userViewModel.Username;
            
            // Set the role - convert role text to index
            _selectedRoleText = userViewModel.Role;
            _selectedRoleIndex = userViewModel.Role switch
            {
                "User" => 0,
                "Manager" => 1,
                "Admin" => 2,
                _ => 0
            };
            RoleButton.Text = _selectedRoleText;
            if (RolePicker.Items.Count > _selectedRoleIndex)
            {
                RolePicker.SelectedIndex = _selectedRoleIndex;
            }
            
            // Show message
            await ToastNotification.ShowAsync("Edit Mode", $"Update '{userViewModel.Name}' details and click Create User to save.", NotificationType.Info, 4000);
            
            // Scroll to top to show the form
            // Note: In a real implementation, you might want to change "Create User" button to "Update User"
            // and track which user is being edited
        }
        catch (Exception ex)
        {
            await ToastNotification.ShowAsync("Error", $"Failed to load user data: {ex.Message}", NotificationType.Error, 4000);
        }
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusLabel.Text = message;
        StatusLabel.TextColor = isError ? Colors.Red : Colors.Green;
        StatusLabel.IsVisible = true;
    }

    private void ClearForm()
    {
        NameEntry.Text = string.Empty;
        PINEntry.Text = string.Empty;
        
        // Reset both picker and button
        RolePicker.SelectedIndex = 0; // Default to User
        RoleButton.Text = "User";
        _selectedRoleText = "User";
        _selectedRoleIndex = 0;
        
        StatusLabel.IsVisible = false;
    }
    
    private void OnPINTextChanged(object sender, TextChangedEventArgs e)
    {
        // No visual dots in the simple layout
        // Just ensure only numeric input
    }

    private static string GetRoleColor(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "#EF4444", // Red
            UserRole.Manager => "#F59E0B", // Orange/Yellow
            UserRole.User => "#10B981", // Green
            _ => "#6B7280" // Gray
        };
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected new virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async void OnCloudSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//cloudsettings");
    }

    private async void OnBusinessSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//businesssettings");
    }

    // Order Number Settings Methods
    private async void LoadOrderNumberSettings()
    {
        try
        {
            var settings = await _orderNumberService.GetCurrentSettingsAsync();
            CurrentPrefixLabel.Text = settings.Prefix;
            TodayOrderCountLabel.Text = settings.TodayCount.ToString();

            var preview = await _orderNumberService.GetNextOrderNumbersPreviewAsync();
            NextCollectionLabel.Text = preview.Collection;
            NextDeliveryLabel.Text = preview.Delivery;
            NextTableLabel.Text = preview.Table;
            NextWebLabel.Text = preview.Web;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load order number settings: {ex.Message}", "OK");
        }
    }

    private async void OnUpdatePrefixClicked(object sender, EventArgs e)
    {
        try
        {
            string newPrefix = PrefixEntry.Text?.Trim().ToUpper() ?? "";

            // Validate input
            if (string.IsNullOrWhiteSpace(newPrefix))
            {
                await DisplayAlert("Error", "Please enter a prefix", "OK");
                return;
            }

            if (newPrefix.Length != 2)
            {
                await DisplayAlert("Error", "Prefix must be exactly 2 letters", "OK");
                return;
            }

            if (!char.IsLetter(newPrefix[0]) || !char.IsLetter(newPrefix[1]))
            {
                await DisplayAlert("Error", "Prefix must contain only letters (A-Z)", "OK");
                return;
            }

            // Confirm change
            bool confirm = await DisplayAlert(
                "Confirm Change",
                $"Are you sure you want to change the order prefix to '{newPrefix}'?\n\n" +
                $"This will affect all new orders immediately.",
                "Yes, Update",
                "Cancel"
            );

            if (!confirm) return;

            // Update prefix
            await _orderNumberService.UpdatePrefixAsync(newPrefix);

            // Reload settings
            LoadOrderNumberSettings();

            // Clear entry
            PrefixEntry.Text = "";

            // Show success message
            await DisplayAlert("Success", "Prefix updated successfully!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to update prefix: {ex.Message}", "OK");
        }
    }
}

public class UserViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string RoleColor { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool CanDelete { get; set; } = true;
}
