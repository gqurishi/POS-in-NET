using POS_in_NET.Models;
using POS_in_NET.Services;

namespace POS_in_NET.Pages;

public partial class CollectionCustomerModal : ContentPage
{
    private readonly CollectionCustomerService _customerService;
    private CollectionCustomer? _selectedCustomer;

    public CollectionCustomerModal()
    {
        InitializeComponent();
        _customerService = new CollectionCustomerService();
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
            var results = await _customerService.SearchCustomersByNameAsync(searchName);

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
        if (e.CurrentSelection.FirstOrDefault() is CollectionCustomer customer)
        {
            _selectedCustomer = customer;
            CustomerNameEntry.Text = customer.Name;
            PhoneNumberEntry.Text = customer.PhoneNumber;
            SearchResultsBorder.IsVisible = false;
        }
    }

    private void OnCustomerTapped(object sender, EventArgs e)
    {
        if (sender is VisualElement element && element.BindingContext is CollectionCustomer customer)
        {
            _selectedCustomer = customer;
            CustomerNameEntry.Text = customer.Name;
            PhoneNumberEntry.Text = customer.PhoneNumber;
            SearchResultsBorder.IsVisible = false;
        }
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        var name = CustomerNameEntry.Text?.Trim();
        var phone = PhoneNumberEntry.Text?.Trim();

        // Default values for walk-in customers
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Walk-in Customer";
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            phone = "N/A";
        }

        try
        {
            // Save or get existing customer
            var customer = await _customerService.SaveCustomerAsync(name, phone);

            if (customer == null)
            {
                await DisplayAlert("Error", "Failed to save customer information", "OK");
                return;
            }

            // Navigate to order placement page with customer info
            var orderPlacementPage = new OrderPlacementPageSimple("COL", 1, "Staff", 1);
            
            // Pass customer info to order placement page
            orderPlacementPage.SetCollectionOrderInfo(customer.Id, customer.Name, customer.PhoneNumber);

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
