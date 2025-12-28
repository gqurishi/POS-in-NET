using Microsoft.Maui.Controls;
using POS_in_NET.Services;
using System;
using System.Collections.ObjectModel;
using MySqlConnector;

namespace POS_in_NET.Pages
{
    public partial class OrderSearchModal : ContentPage
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<SearchResultItem> SearchResults { get; set; } = new();

        public event Action<string, DateTime>? OrderSelected;

        public OrderSearchModal()
        {
            InitializeComponent();
            
            _databaseService = new DatabaseService();
            SearchResultsCollection.ItemsSource = SearchResults;
        }

        private async void OnSearchClicked(object sender, EventArgs e)
        {
            var orderNumber = OrderNumberEntry.Text?.Trim() ?? "";
            var phoneNumber = PhoneNumberEntry.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(orderNumber) && string.IsNullOrEmpty(phoneNumber))
            {
                await DisplayAlert("Search", "Please enter an Order Number or Phone Number", "OK");
                return;
            }

            try
            {
                SearchResults.Clear();
                
                using var connection = await _databaseService.GetConnectionAsync();
                
                // Build search query
                var conditions = new System.Collections.Generic.List<string>();
                
                if (!string.IsNullOrEmpty(orderNumber))
                {
                    conditions.Add("o.order_id LIKE @orderNumber");
                }
                
                if (!string.IsNullOrEmpty(phoneNumber))
                {
                    conditions.Add("o.customer_phone LIKE @phoneNumber");
                }
                
                var whereClause = string.Join(" OR ", conditions);
                
                var query = $@"
                    SELECT o.id, o.order_id, o.order_type, o.total_amount, o.created_at
                    FROM orders o
                    WHERE ({whereClause})
                    AND o.status IN ('completed', 'closed', 'paid', 'void', 'cancelled')
                    ORDER BY o.created_at DESC
                    LIMIT 50";
                
                using var command = new MySqlCommand(query, connection);
                
                if (!string.IsNullOrEmpty(orderNumber))
                {
                    command.Parameters.AddWithValue("@orderNumber", $"%{orderNumber}%");
                }
                
                if (!string.IsNullOrEmpty(phoneNumber))
                {
                    command.Parameters.AddWithValue("@phoneNumber", $"%{phoneNumber}%");
                }
                
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var orderType = reader.GetString("order_type");
                    var createdAt = reader.GetDateTime("created_at");
                    
                    SearchResults.Add(new SearchResultItem
                    {
                        Id = reader.GetInt32("id"),
                        OrderId = reader.GetString("order_id"),
                        OrderType = orderType,
                        OrderIcon = GetOrderIcon(orderType),
                        TotalAmount = reader.GetDecimal("total_amount"),
                        CreatedAt = createdAt,
                        OrderDateTime = $"{createdAt:h:mm tt} • {createdAt:dd/MM/yyyy}"
                    });
                }
                
                ResultsLayout.IsVisible = true;
                NoResultsLabel.IsVisible = SearchResults.Count == 0;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Search failed: {ex.Message}", "OK");
            }
        }

        private string GetOrderIcon(string orderType)
        {
            return orderType switch
            {
                "COL" => "📦",
                "DEL" => "🚗",
                "TBL" => "🍽",
                _ => "📋"
            };
        }

        private async void OnResultSelected(object sender, EventArgs e)
        {
            if (sender is VisualElement element && element.BindingContext is SearchResultItem result)
            {
                OrderSelected?.Invoke(result.OrderId, result.CreatedAt.Date);
                await Navigation.PopModalAsync();
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }

    public class SearchResultItem
    {
        public int Id { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
        public string OrderIcon { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string OrderDateTime { get; set; } = string.Empty;
    }
}
