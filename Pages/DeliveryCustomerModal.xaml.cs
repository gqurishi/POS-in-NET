using POS_in_NET.Models;
using POS_in_NET.Services;

namespace POS_in_NET.Pages;

public partial class DeliveryCustomerModal : ContentPage
{
    private readonly DeliveryCustomerService _customerService;
    private readonly PostcodeLookupService _postcodeLookupService;
    private DeliveryCustomer? _selectedCustomer;

    public DeliveryCustomerModal()
    {
        InitializeComponent();
        _customerService = new DeliveryCustomerService();
        _postcodeLookupService = new PostcodeLookupService(new DatabaseService());
    }

    private async void OnSearchPostcodeClicked(object sender, EventArgs e)
    {
        var postcode = PostcodeEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(postcode))
        {
            await DisplayAlert("Postcode Required", "Please enter a postcode to search", "OK");
            return;
        }

        // Show loading
        SearchPostcodeButton.Text = "Searching...";
        SearchPostcodeButton.IsEnabled = false;

        try
        {
            var addresses = await _postcodeLookupService.LookupPostcodeAsync(postcode);

            if (addresses.Any())
            {
                AddressResultsCollection.ItemsSource = addresses;
                AddressResultsBorder.IsVisible = true;
            }
            else
            {
                AddressResultsBorder.IsVisible = false;
                await DisplayAlert("No Results", $"No addresses found for postcode: {postcode}", "OK");
            }
        }
        catch (Exception ex)
        {
            AddressResultsBorder.IsVisible = false;
            await DisplayAlert("Error", $"Failed to lookup postcode: {ex.Message}\n\nMake sure Mapbox is configured in Settings - Postcode Lookup", "OK");
        }
        finally
        {
            SearchPostcodeButton.Text = "Search";
            SearchPostcodeButton.IsEnabled = true;
        }
    }

    private void OnAddressSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is AddressResult address)
        {
            // Fill structured address fields
            AddressLine1Entry.Text = address.AddressLine1;
            CityEntry.Text = address.City;
            CountyEntry.Text = address.County;
            PostcodeResultEntry.Text = address.Postcode;
            
            AddressResultsBorder.IsVisible = false;
        }
    }

    private async void OnSearchClicked(object sender, EventArgs e)
    {
        var searchName = CustomerNameEntry.Text?.Trim();
        var searchPhone = PhoneNumberEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(searchName) && string.IsNullOrWhiteSpace(searchPhone))
        {
            await DisplayAlert("Search", "Please enter a customer name or phone number to search", "OK");
            return;
        }

        // Show loading
        var button = (Button)sender;
        var originalText = button.Text;
        button.Text = "Searching...";
        button.IsEnabled = false;

        try
        {
            var results = await _customerService.SearchCustomersByNameAsync(searchName ?? searchPhone ?? "");

            if (results.Any())
            {
                SearchResultsCollection.ItemsSource = results;
                SearchResultsBorder.IsVisible = true;
                NoResultsLabel.IsVisible = false;
            }
            else
            {
                SearchResultsBorder.IsVisible = false;
                NoResultsLabel.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to search customers: {ex.Message}", "OK");
        }
        finally
        {
            button.Text = originalText;
            button.IsEnabled = true;
        }
    }

    private void OnCustomerSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is DeliveryCustomer customer)
        {
            _selectedCustomer = customer;
            CustomerNameEntry.Text = customer.Name;
            PhoneNumberEntry.Text = customer.PhoneNumber;
            
            // Parse address - assuming it's stored as single string
            var addressLines = customer.Address?.Split('\n') ?? Array.Empty<string>();
            if (addressLines.Length > 0) AddressLine1Entry.Text = addressLines[0];
            if (addressLines.Length > 1) CityEntry.Text = addressLines[1];
            if (addressLines.Length > 2) PostcodeResultEntry.Text = addressLines[2];
            
            SearchResultsBorder.IsVisible = false;
        }
    }

    private void OnCustomerTapped(object sender, EventArgs e)
    {
        if (sender is VisualElement element && element.BindingContext is DeliveryCustomer customer)
        {
            _selectedCustomer = customer;
            CustomerNameEntry.Text = customer.Name;
            PhoneNumberEntry.Text = customer.PhoneNumber;
            
            // Parse address
            var addressLines = customer.Address?.Split('\n') ?? Array.Empty<string>();
            if (addressLines.Length > 0) AddressLine1Entry.Text = addressLines[0];
            if (addressLines.Length > 1) CityEntry.Text = addressLines[1];
            if (addressLines.Length > 2) PostcodeResultEntry.Text = addressLines[2];
            
            SearchResultsBorder.IsVisible = false;
        }
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        var name = CustomerNameEntry.Text?.Trim();
        var phone = PhoneNumberEntry.Text?.Trim();
        
        // Build address from structured fields
        var addressParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(AddressLine1Entry.Text)) addressParts.Add(AddressLine1Entry.Text.Trim());
        if (!string.IsNullOrWhiteSpace(CityEntry.Text)) addressParts.Add(CityEntry.Text.Trim());
        if (!string.IsNullOrWhiteSpace(CountyEntry.Text)) addressParts.Add(CountyEntry.Text.Trim());
        if (!string.IsNullOrWhiteSpace(PostcodeResultEntry.Text)) addressParts.Add(PostcodeResultEntry.Text.Trim());
        
        var address = string.Join("\n", addressParts);

        // Default values for walk-in customers
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Delivery Customer";
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            phone = "N/A";
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            await DisplayAlert("Address Required", "Please enter a delivery address", "OK");
            return;
        }

        try
        {
            // Save or get existing customer
            var customer = await _customerService.SaveCustomerAsync(name, phone, address);

            if (customer == null)
            {
                await DisplayAlert("Error", "Failed to save customer information", "OK");
                return;
            }

            // Navigate to order placement page with customer info
            var orderPlacementPage = new OrderPlacementPageSimple("DEL", 1, "Staff", 1);
            
            // Pass customer info to order placement page
            orderPlacementPage.SetDeliveryOrderInfo(customer.Id, customer.Name, customer.PhoneNumber, customer.Address);

            await Navigation.PushAsync(orderPlacementPage);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to proceed: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        // Navigate back to Dashboard instead of just closing the modal
        await Shell.Current.GoToAsync("//dashboard");
    }
}
