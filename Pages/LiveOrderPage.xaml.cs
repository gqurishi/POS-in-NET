using Microsoft.Maui.Controls;
using POS_in_NET.Models;
using POS_in_NET.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MySqlConnector;

namespace POS_in_NET.Pages
{
    public partial class LiveOrderPage : ContentPage
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<Order> CollectionOrders { get; set; } = new();
        private ObservableCollection<Order> DeliveryOrders { get; set; } = new();
        private ObservableCollection<TableSession> TableSessions { get; set; } = new();

        public LiveOrderPage()
        {
            InitializeComponent();
            
            _databaseService = new DatabaseService();
            
            // Set page title
            TopBar.SetPageTitle("Live Orders");
            
            CollectionOrdersCollection.ItemsSource = CollectionOrders;
            DeliveryOrdersCollection.ItemsSource = DeliveryOrders;
            TableOrdersCollection.ItemsSource = TableSessions;
            
            // Set initial tab selection (Table is default)
            OnTableTabClicked(null, null);
            
            LoadAllOrders();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadAllOrders();
        }

        private async void LoadAllOrders()
        {
            await LoadCollectionOrdersAsync();
            await LoadDeliveryOrdersAsync();
            await LoadTableSessionsAsync();
        }

        private async Task LoadCollectionOrdersAsync()
        {
            try
            {
                CollectionOrders.Clear();
                
                using var connection = await _databaseService.GetConnectionAsync();
                var query = @"
                    SELECT o.id, o.order_id as OrderNumber, o.customer_name as CustomerName, 
                           o.total_amount as TotalAmount, o.created_at as CreatedAt
                    FROM orders o
                    WHERE o.order_type = 'COL' 
                    AND o.status NOT IN ('completed', 'cancelled', 'void')
                    ORDER BY o.created_at DESC";
                
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    CollectionOrders.Add(new Order
                    {
                        Id = reader.GetInt32("id"),
                        OrderId = reader.IsDBNull(reader.GetOrdinal("OrderNumber")) ? "" : reader.GetString("OrderNumber"),
                        CustomerName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? "Guest" : reader.GetString("CustomerName"),
                        TotalAmount = reader.IsDBNull(reader.GetOrdinal("TotalAmount")) ? 0 : reader.GetDecimal("TotalAmount"),
                        CreatedAt = reader.GetDateTime("CreatedAt")
                    });
                }
                
                CollectionEmptyLabel.IsVisible = CollectionOrders.Count == 0;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load collection orders: {ex.Message}", "OK");
            }
        }

        private async Task LoadDeliveryOrdersAsync()
        {
            try
            {
                DeliveryOrders.Clear();
                
                using var connection = await _databaseService.GetConnectionAsync();
                var query = @"
                    SELECT o.id, o.order_id as OrderNumber, o.customer_name as CustomerName, 
                           o.total_amount as TotalAmount, o.created_at as CreatedAt
                    FROM orders o
                    WHERE o.order_type = 'DEL' 
                    AND o.status NOT IN ('completed', 'cancelled', 'void')
                    ORDER BY o.created_at DESC";
                
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    DeliveryOrders.Add(new Order
                    {
                        Id = reader.GetInt32("id"),
                        OrderId = reader.IsDBNull(reader.GetOrdinal("OrderNumber")) ? "" : reader.GetString("OrderNumber"),
                        CustomerName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? "Guest" : reader.GetString("CustomerName"),
                        TotalAmount = reader.IsDBNull(reader.GetOrdinal("TotalAmount")) ? 0 : reader.GetDecimal("TotalAmount"),
                        CreatedAt = reader.GetDateTime("CreatedAt")
                    });
                }
                
                DeliveryEmptyLabel.IsVisible = DeliveryOrders.Count == 0;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load delivery orders: {ex.Message}", "OK");
            }
        }

        private async Task LoadTableSessionsAsync()
        {
            try
            {
                TableSessions.Clear();
                
                using var connection = await _databaseService.GetConnectionAsync();
                var query = @"
                    SELECT ts.Id, ts.TableId, ts.SessionNumber, ts.PartySize, 
                           ts.StartTime, ts.Status, rt.TableNumber
                    FROM TableSessions ts
                    LEFT JOIN RestaurantTables rt ON ts.TableId = rt.Id
                    WHERE ts.Status != 'Closed' AND ts.IsActive = 1
                    ORDER BY ts.StartTime DESC";
                
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var session = new TableSession
                    {
                        Id = reader.GetInt32("Id"),
                        TableId = reader.GetInt32("TableId"),
                        SessionNumber = reader.GetString("SessionNumber"),
                        PartySize = reader.GetInt32("PartySize"),
                        StartTime = reader.GetDateTime("StartTime"),
                        Status = Enum.Parse<TableSessionStatus>(reader.GetString("Status"))
                    };
                    
                    // Create a navigation property for table display
                    if (!reader.IsDBNull(reader.GetOrdinal("TableNumber")))
                    {
                        session.Table = new RestaurantTable
                        {
                            TableNumber = reader.GetString("TableNumber")
                        };
                    }
                    
                    TableSessions.Add(session);
                }
                
                TableEmptyLabel.IsVisible = TableSessions.Count == 0;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load table sessions: {ex.Message}", "OK");
            }
        }

        private void OnCollectionTabClicked(object sender, EventArgs e)
        {
            // Highlight Collection tab with green
            CollectionTabBorder.BackgroundColor = Color.FromArgb("#10B981"); // Green
            DeliveryTabBorder.BackgroundColor = Color.FromArgb("#F5F5F5");
            TableTabBorder.BackgroundColor = Color.FromArgb("#F5F5F5");
            
            // Update label colors
            CollectionTabLabel.TextColor = Colors.White;
            DeliveryTabLabel.TextColor = Color.FromArgb("#6B7280");
            TableTabLabel.TextColor = Color.FromArgb("#6B7280");
            
            // Show Collection view
            CollectionOrdersLayout.IsVisible = true;
            DeliveryOrdersLayout.IsVisible = false;
            TableOrdersLayout.IsVisible = false;
        }

        private void OnDeliveryTabClicked(object sender, EventArgs e)
        {
            // Highlight Delivery tab with green
            CollectionTabBorder.BackgroundColor = Color.FromArgb("#F5F5F5");
            DeliveryTabBorder.BackgroundColor = Color.FromArgb("#10B981"); // Green
            TableTabBorder.BackgroundColor = Color.FromArgb("#F5F5F5");
            
            // Update label colors
            CollectionTabLabel.TextColor = Color.FromArgb("#6B7280");
            DeliveryTabLabel.TextColor = Colors.White;
            TableTabLabel.TextColor = Color.FromArgb("#6B7280");
            
            // Show Delivery view
            CollectionOrdersLayout.IsVisible = false;
            DeliveryOrdersLayout.IsVisible = true;
            TableOrdersLayout.IsVisible = false;
        }

        private void OnTableTabClicked(object sender, EventArgs e)
        {
            // Highlight Table tab with green
            CollectionTabBorder.BackgroundColor = Color.FromArgb("#F5F5F5");
            DeliveryTabBorder.BackgroundColor = Color.FromArgb("#F5F5F5");
            TableTabBorder.BackgroundColor = Color.FromArgb("#10B981"); // Green
            
            // Update label colors
            CollectionTabLabel.TextColor = Color.FromArgb("#6B7280");
            DeliveryTabLabel.TextColor = Color.FromArgb("#6B7280");
            TableTabLabel.TextColor = Colors.White;
            
            // Show Table view
            CollectionOrdersLayout.IsVisible = false;
            DeliveryOrdersLayout.IsVisible = false;
            TableOrdersLayout.IsVisible = true;
        }

        private async void OnOrderTapped(object sender, EventArgs e)
        {
            if (sender is VisualElement element && element.BindingContext is Order order)
            {
                // Navigate to order edit page
                var orderPage = new OrderPlacementPageSimple();
                
                // Load the existing order - TODO: Add LoadOrderById method to OrderPlacementPageSimple
                
                await Navigation.PushAsync(orderPage);
            }
        }

        private async void OnTableTapped(object sender, EventArgs e)
        {
            if (sender is VisualElement element && element.BindingContext is TableSession session)
            {
                // Navigate to table order page
                // OrderPlacementPageSimple constructor: (string tableNumber, int coverCount, string staffName, int staffId)
                var tableName = session.Table?.TableNumber ?? session.TableId.ToString();
                var orderPage = new OrderPlacementPageSimple(
                    tableName, 
                    session.PartySize, 
                    "Staff", 
                    1
                );
                
                await Navigation.PushAsync(orderPage);
            }
        }
    }
}
