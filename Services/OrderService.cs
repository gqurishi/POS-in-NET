using MySqlConnector;
using POS_in_NET.Models;
using System.Text.Json;

namespace POS_in_NET.Services;

public class OrderService
{
    private readonly string _connectionString;
    private readonly OnlineOrderApiService _apiService;

    public OrderService()
    {
        _connectionString = "Server=localhost;Database=Pos-net;Uid=root;Pwd=root;Port=3306;";
        _apiService = new OnlineOrderApiService();
    }

    public async Task<(bool Success, string Message)> SaveOrderAsync(Order order)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Check if order already exists
            var existingOrder = await GetOrderByExternalIdAsync(order.OrderId);
            if (existingOrder != null)
            {
                return await UpdateOrderAsync(order);
            }

            // Insert new order with all OrderWeb.net fields
            var orderQuery = @"
                INSERT INTO orders (order_id, order_number, cloud_order_id, 
                                  customer_name, customer_phone, customer_email, customer_address, 
                                  total_amount, subtotal_amount, delivery_fee, tax_amount,
                                  order_type, payment_method, special_instructions, scheduled_time,
                                  status, order_data, sync_status, 
                                  kitchen_time, preparing_time, ready_time, delivering_time, completed_time,
                                  created_at, updated_at) 
                VALUES (@orderId, @orderNumber, @cloudOrderId,
                        @customerName, @customerPhone, @customerEmail, @customerAddress, 
                        @totalAmount, @subtotalAmount, @deliveryFee, @taxAmount,
                        @orderType, @paymentMethod, @specialInstructions, @scheduledTime,
                        @status, @orderData, @syncStatus,
                        @kitchenTime, @preparingTime, @readyTime, @deliveringTime, @completedTime,
                        @createdAt, @updatedAt)";

            using var orderCommand = new MySqlCommand(orderQuery, connection);
            orderCommand.Parameters.AddWithValue("@orderId", order.OrderId);
            orderCommand.Parameters.AddWithValue("@orderNumber", order.OrderNumber ?? (object)DBNull.Value);
            orderCommand.Parameters.AddWithValue("@cloudOrderId", order.CloudOrderId ?? (object)DBNull.Value);
            orderCommand.Parameters.AddWithValue("@customerName", order.CustomerName);
            orderCommand.Parameters.AddWithValue("@customerPhone", order.CustomerPhone ?? (object)DBNull.Value);
            orderCommand.Parameters.AddWithValue("@customerEmail", order.CustomerEmail ?? (object)DBNull.Value);
            orderCommand.Parameters.AddWithValue("@customerAddress", order.CustomerAddress ?? (object)DBNull.Value);
            orderCommand.Parameters.AddWithValue("@totalAmount", order.TotalAmount);
            orderCommand.Parameters.AddWithValue("@subtotalAmount", order.SubtotalAmount);
            orderCommand.Parameters.AddWithValue("@deliveryFee", order.DeliveryFee);
            orderCommand.Parameters.AddWithValue("@taxAmount", order.TaxAmount);
            orderCommand.Parameters.AddWithValue("@orderType", order.OrderType ?? (object)DBNull.Value);
            orderCommand.Parameters.AddWithValue("@paymentMethod", order.PaymentMethod ?? (object)DBNull.Value);
            orderCommand.Parameters.AddWithValue("@specialInstructions", order.SpecialInstructions ?? (object)DBNull.Value);
            orderCommand.Parameters.AddWithValue("@scheduledTime", order.ScheduledTime ?? (object)DBNull.Value);
            orderCommand.Parameters.AddWithValue("@status", order.Status.ToString().ToLower());
            orderCommand.Parameters.AddWithValue("@orderData", order.OrderData ?? (object)DBNull.Value);
            orderCommand.Parameters.AddWithValue("@syncStatus", order.SyncStatus.ToString().ToLower());
            orderCommand.Parameters.AddWithValue("@kitchenTime", order.KitchenTime ?? (object)DBNull.Value);
            orderCommand.Parameters.AddWithValue("@preparingTime", order.PreparingTime ?? (object)DBNull.Value);
            orderCommand.Parameters.AddWithValue("@readyTime", order.ReadyTime ?? (object)DBNull.Value);
            orderCommand.Parameters.AddWithValue("@deliveringTime", order.DeliveringTime ?? (object)DBNull.Value);
            orderCommand.Parameters.AddWithValue("@completedTime", order.CompletedTime ?? (object)DBNull.Value);
            orderCommand.Parameters.AddWithValue("@createdAt", order.CreatedAt);
            orderCommand.Parameters.AddWithValue("@updatedAt", DateTime.Now);

            await orderCommand.ExecuteNonQueryAsync();

            // Get the inserted order ID
            var getIdQuery = "SELECT LAST_INSERT_ID()";
            using var idCommand = new MySqlCommand(getIdQuery, connection);
            var insertedId = Convert.ToInt32(await idCommand.ExecuteScalarAsync());
            order.Id = insertedId;

            // Insert order items
            foreach (var item in order.Items)
            {
                await SaveOrderItemAsync(connection, insertedId, item);
            }

            return (true, "Order saved successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to save order: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> UpdateOrderAsync(Order order)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var updateQuery = @"
                UPDATE orders SET 
                    customer_name = @customerName,
                    customer_phone = @customerPhone,
                    customer_address = @customerAddress,
                    total_amount = @totalAmount,
                    status = @status,
                    sync_status = @syncStatus,
                    kitchen_time = @kitchenTime,
                    preparing_time = @preparingTime,
                    ready_time = @readyTime,
                    delivering_time = @deliveringTime,
                    completed_time = @completedTime,
                    updated_at = @updatedAt
                WHERE order_id = @orderId";

            using var command = new MySqlCommand(updateQuery, connection);
            command.Parameters.AddWithValue("@customerName", order.CustomerName);
            command.Parameters.AddWithValue("@customerPhone", order.CustomerPhone ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@customerAddress", order.CustomerAddress ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@totalAmount", order.TotalAmount);
            command.Parameters.AddWithValue("@status", order.Status.ToString().ToLower());
            command.Parameters.AddWithValue("@syncStatus", order.SyncStatus.ToString().ToLower());
            command.Parameters.AddWithValue("@kitchenTime", order.KitchenTime ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@preparingTime", order.PreparingTime ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@readyTime", order.ReadyTime ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@deliveringTime", order.DeliveringTime ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@completedTime", order.CompletedTime ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@updatedAt", DateTime.Now);
            command.Parameters.AddWithValue("@orderId", order.OrderId);

            await command.ExecuteNonQueryAsync();

            return (true, "Order updated successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to update order: {ex.Message}");
        }
    }

    public async Task<Order?> GetOrderByExternalIdAsync(string externalOrderId)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT * FROM orders WHERE order_id = @orderId";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@orderId", externalOrderId);

            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var order = MapOrderFromReader((MySqlDataReader)reader);
                await reader.CloseAsync();
                
                // Load order items
                order.Items = await GetOrderItemsAsync(connection, order.Id);
                
                return order;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting order: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT * FROM orders WHERE status = @status ORDER BY created_at ASC";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@status", status.ToString().ToLower());

            var orders = new List<Order>();
            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                orders.Add(MapOrderFromReader((MySqlDataReader)reader));
            }
            await reader.CloseAsync();

            // Load items for each order
            foreach (var order in orders)
            {
                order.Items = await GetOrderItemsAsync(connection, order.Id);
            }

            return orders;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting orders by status: {ex.Message}");
            return new List<Order>();
        }
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var orders = new List<Order>();

        const string sql = @"
            SELECT id, order_id, order_number, cloud_order_id,
                   customer_name, customer_phone, customer_email, customer_address, 
                   total_amount, subtotal_amount, delivery_fee, tax_amount,
                   order_type, payment_method, special_instructions, scheduled_time,
                   status, order_data, sync_status, 
                   kitchen_time, preparing_time, ready_time, delivering_time, completed_time,
                   created_at, updated_at
            FROM orders 
            ORDER BY created_at DESC";

        using var command = new MySqlCommand(sql, connection);
        using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            orders.Add(MapOrderFromReader((MySqlDataReader)reader));
        }

        await reader.CloseAsync();

        // Load order items for each order
        foreach (var order in orders)
        {
            order.Items = await GetOrderItemsAsync(connection, order.Id);
        }

        return orders;
    }

    private async Task<Order?> GetOrderByIdAsync(MySqlConnection connection, int orderId)
    {
        const string sql = @"
            SELECT id, order_id, order_number, cloud_order_id,
                   customer_name, customer_phone, customer_email, customer_address, 
                   total_amount, subtotal_amount, delivery_fee, tax_amount,
                   order_type, payment_method, special_instructions, scheduled_time,
                   status, order_data, sync_status, 
                   kitchen_time, preparing_time, ready_time, delivering_time, completed_time,
                   created_at, updated_at
            FROM orders 
            WHERE id = @id";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", orderId);
        
        using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var order = MapOrderFromReader(reader);
            await reader.CloseAsync();
            
            // Load order items
            order.Items = await GetOrderItemsAsync(connection, order.Id);
            
            return order;
        }
        
        return null;
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            
            // Get the current order to update status timestamps
            const string selectSql = "SELECT status FROM orders WHERE id = @id";
            using var selectCommand = new MySqlCommand(selectSql, connection);
            selectCommand.Parameters.AddWithValue("@id", orderId);
            
            var currentStatusObj = await selectCommand.ExecuteScalarAsync();
            if (currentStatusObj == null) return false;
            
            // Determine which timestamp column to update based on new status
            var timestampColumn = newStatus switch
            {
                OrderStatus.Kitchen => "kitchen_time",
                OrderStatus.Preparing => "preparing_time",
                OrderStatus.Ready => "ready_time", 
                OrderStatus.Delivering => "delivering_time",
                OrderStatus.Completed => "completed_time",
                _ => null
            };

            // Update the status and appropriate timestamp
            var updateSql = timestampColumn != null 
                ? $"UPDATE orders SET status = @status, {timestampColumn} = @timestamp, updated_at = @updated WHERE id = @id"
                : "UPDATE orders SET status = @status, updated_at = @updated WHERE id = @id";

            using var updateCommand = new MySqlCommand(updateSql, connection);
            updateCommand.Parameters.AddWithValue("@id", orderId);
            updateCommand.Parameters.AddWithValue("@status", newStatus.ToString());
            updateCommand.Parameters.AddWithValue("@updated", DateTime.Now);
            
            if (timestampColumn != null)
            {
                updateCommand.Parameters.AddWithValue("@timestamp", DateTime.Now);
            }

            var rowsAffected = await updateCommand.ExecuteNonQueryAsync();
            
            return rowsAffected > 0;
        }
        catch (Exception)
        {
            // Log error if needed
            return false;
        }
    }

    public async Task<Dictionary<OrderStatus, int>> GetOrderCountsByStatusAsync()
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT status, COUNT(*) as count FROM orders 
                         WHERE status != 'completed' AND status != 'cancelled'
                         GROUP BY status";
            
            using var command = new MySqlCommand(query, connection);
            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            var counts = new Dictionary<OrderStatus, int>();
            
            while (await reader.ReadAsync())
            {
                var statusStr = reader["status"].ToString() ?? "";
                var count = Convert.ToInt32(reader["count"]);
                
                if (Enum.TryParse<OrderStatus>(statusStr, true, out var status))
                {
                    counts[status] = count;
                }
            }

            return counts;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting order counts: {ex.Message}");
            return new Dictionary<OrderStatus, int>();
        }
    }

    public async Task<int> GetTodayCompletedOrdersCountAsync()
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"SELECT COUNT(*) FROM orders 
                         WHERE status = 'completed' 
                         AND DATE(completed_time) = CURDATE()";
            
            using var command = new MySqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();
            
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting today's completed orders: {ex.Message}");
            return 0;
        }
    }

    public async Task<(bool Success, string Message)> ProcessAutomaticStatusTransitionsAsync()
    {
        try
        {
            var now = DateTime.Now;
            var updatedOrders = 0;

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Kitchen -> Preparing (after 2 minutes)
            var kitchenToPreparingQuery = @"
                UPDATE orders SET 
                    status = 'preparing',
                    preparing_time = @now,
                    updated_at = @now
                WHERE status = 'kitchen' 
                AND kitchen_time IS NOT NULL 
                AND TIMESTAMPDIFF(MINUTE, kitchen_time, @now) >= 2";

            using var cmd1 = new MySqlCommand(kitchenToPreparingQuery, connection);
            cmd1.Parameters.AddWithValue("@now", now);
            var count1 = await cmd1.ExecuteNonQueryAsync();
            updatedOrders += count1;

            // Preparing -> Ready (after 10 minutes total from kitchen time)
            var preparingToReadyQuery = @"
                UPDATE orders SET 
                    status = 'ready',
                    ready_time = @now,
                    updated_at = @now
                WHERE status = 'preparing' 
                AND kitchen_time IS NOT NULL 
                AND TIMESTAMPDIFF(MINUTE, kitchen_time, @now) >= 10";

            using var cmd2 = new MySqlCommand(preparingToReadyQuery, connection);
            cmd2.Parameters.AddWithValue("@now", now);
            var count2 = await cmd2.ExecuteNonQueryAsync();
            updatedOrders += count2;

            return (true, $"Processed {updatedOrders} automatic status transitions");
        }
        catch (Exception ex)
        {
            return (false, $"Error processing status transitions: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> MarkOrderForDeliveryAsync(string orderId, string deliveryPersonName)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE orders SET 
                    status = 'delivering',
                    delivering_time = @deliveringTime,
                    updated_at = @updatedAt
                WHERE order_id = @orderId AND status = 'ready'";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@deliveringTime", DateTime.Now);
            command.Parameters.AddWithValue("@updatedAt", DateTime.Now);
            command.Parameters.AddWithValue("@orderId", orderId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            if (rowsAffected > 0)
            {
                // Try to sync status with online system
                await _apiService.UpdateOrderStatusAsync(orderId, OrderStatus.Delivering, $"Taken by: {deliveryPersonName}");
                
                return (true, "Order marked for delivery successfully");
            }
            else
            {
                return (false, "Order not found or not in ready status");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error marking order for delivery: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> CompleteOrderAsync(string orderId)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE orders SET 
                    status = 'completed',
                    completed_time = @completedTime,
                    updated_at = @updatedAt
                WHERE order_id = @orderId AND status = 'delivering'";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@completedTime", DateTime.Now);
            command.Parameters.AddWithValue("@updatedAt", DateTime.Now);
            command.Parameters.AddWithValue("@orderId", orderId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            if (rowsAffected > 0)
            {
                // Try to sync status with online system
                await _apiService.UpdateOrderStatusAsync(orderId, OrderStatus.Completed, "Delivered successfully");
                
                return (true, "Order completed successfully");
            }
            else
            {
                return (false, "Order not found or not in delivering status");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error completing order: {ex.Message}");
        }
    }

    private async Task SaveOrderItemAsync(MySqlConnection connection, int orderId, OrderItem item)
    {
        // Insert order item with new schema
        var itemQuery = @"
            INSERT INTO order_items (order_id, cloud_item_id, menu_item_id, item_name, quantity, item_price, special_instructions) 
            VALUES (@orderId, @cloudItemId, @menuItemId, @itemName, @quantity, @itemPrice, @specialInstructions)";

        using var itemCommand = new MySqlCommand(itemQuery, connection);
        itemCommand.Parameters.AddWithValue("@orderId", item.OrderId); // Use string OrderId from item
        itemCommand.Parameters.AddWithValue("@cloudItemId", item.CloudItemId ?? (object)DBNull.Value);
        itemCommand.Parameters.AddWithValue("@menuItemId", item.MenuItemId ?? (object)DBNull.Value);
        itemCommand.Parameters.AddWithValue("@itemName", item.ItemName);
        itemCommand.Parameters.AddWithValue("@quantity", item.Quantity);
        itemCommand.Parameters.AddWithValue("@itemPrice", item.ItemPrice ?? (object)DBNull.Value);
        itemCommand.Parameters.AddWithValue("@specialInstructions", item.SpecialInstructions ?? (object)DBNull.Value);

        await itemCommand.ExecuteNonQueryAsync();

        // Get the inserted item ID
        var getItemIdQuery = "SELECT LAST_INSERT_ID()";
        using var idCommand = new MySqlCommand(getItemIdQuery, connection);
        var itemId = Convert.ToInt32(await idCommand.ExecuteScalarAsync());

        // Insert addons for this item
        foreach (var addon in item.Addons)
        {
            await SaveOrderItemAddonAsync(connection, itemId, addon);
        }
    }

    private async Task SaveOrderItemAddonAsync(MySqlConnection connection, int orderItemId, OrderItemAddon addon)
    {
        var addonQuery = @"
            INSERT INTO order_item_addons (order_item_id, addon_id, addon_name, addon_price, quantity) 
            VALUES (@orderItemId, @addonId, @addonName, @addonPrice, @quantity)";

        using var addonCommand = new MySqlCommand(addonQuery, connection);
        addonCommand.Parameters.AddWithValue("@orderItemId", orderItemId);
        addonCommand.Parameters.AddWithValue("@addonId", addon.AddonId ?? (object)DBNull.Value);
        addonCommand.Parameters.AddWithValue("@addonName", addon.AddonName);
        addonCommand.Parameters.AddWithValue("@addonPrice", addon.AddonPrice ?? (object)DBNull.Value);
        addonCommand.Parameters.AddWithValue("@quantity", addon.Quantity);

        await addonCommand.ExecuteNonQueryAsync();
    }

    private async Task<List<OrderItem>> GetOrderItemsAsync(MySqlConnection connection, int orderId)
    {
        var items = new List<OrderItem>();
        
        var query = "SELECT * FROM order_items WHERE order_id = @orderId";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@orderId", orderId);

        using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var item = new OrderItem
            {
                Id = Convert.ToInt32(reader["id"]),
                OrderId = reader["order_id"].ToString() ?? "",
                CloudItemId = reader["cloud_item_id"] as int?,
                MenuItemId = reader["menu_item_id"]?.ToString(),
                ItemName = reader["item_name"].ToString() ?? "",
                Quantity = Convert.ToInt32(reader["quantity"]),
                ItemPrice = reader["item_price"] as decimal?,
                SpecialInstructions = reader["special_instructions"]?.ToString(),
                Addons = new List<OrderItemAddon>() // Will be loaded separately
            };
            items.Add(item);
        }

        return items;
    }

    private Order MapOrderFromReader(MySqlDataReader reader)
    {
        return new Order
        {
            Id = Convert.ToInt32(reader["id"]),
            OrderId = reader["order_id"].ToString() ?? "",
            OrderNumber = reader["order_number"]?.ToString(),
            CloudOrderId = reader["cloud_order_id"]?.ToString(),
            
            // Customer information
            CustomerName = reader["customer_name"].ToString() ?? "",
            CustomerPhone = reader["customer_phone"]?.ToString(),
            CustomerEmail = reader["customer_email"]?.ToString(),
            CustomerAddress = reader["customer_address"]?.ToString(),
            
            // Financial information
            TotalAmount = reader["total_amount"] != DBNull.Value ? Convert.ToDecimal(reader["total_amount"]) : 0,
            SubtotalAmount = reader["subtotal_amount"] != DBNull.Value ? Convert.ToDecimal(reader["subtotal_amount"]) : 0,
            DeliveryFee = reader["delivery_fee"] != DBNull.Value ? Convert.ToDecimal(reader["delivery_fee"]) : 0,
            TaxAmount = reader["tax_amount"] != DBNull.Value ? Convert.ToDecimal(reader["tax_amount"]) : 0,
            
            // Order details
            OrderType = reader["order_type"]?.ToString(),
            PaymentMethod = reader["payment_method"]?.ToString(),
            SpecialInstructions = reader["special_instructions"]?.ToString(),
            ScheduledTime = reader["scheduled_time"] as DateTime?,
            
            // Status and timing
            Status = Enum.Parse<OrderStatus>(reader["status"].ToString() ?? "New", true),
            SyncStatus = Enum.Parse<Models.SyncStatus>(reader["sync_status"].ToString() ?? "Pending", true),
            KitchenTime = reader["kitchen_time"] as DateTime?,
            PreparingTime = reader["preparing_time"] as DateTime?,
            ReadyTime = reader["ready_time"] as DateTime?,
            DeliveringTime = reader["delivering_time"] as DateTime?,
            CompletedTime = reader["completed_time"] as DateTime?,
            CreatedAt = Convert.ToDateTime(reader["created_at"]),
            UpdatedAt = Convert.ToDateTime(reader["updated_at"]),
            OrderData = reader["order_data"]?.ToString()
        };
    }
}