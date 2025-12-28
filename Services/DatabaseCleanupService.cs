using MySqlConnector;
using System.Diagnostics;

namespace POS_in_NET.Services;

/// <summary>
/// Automatically cleans up old OrderWeb.net orders (3+ months old)
/// NEVER deletes local POS orders - only cloud-synced web orders
/// </summary>
public class DatabaseCleanupService
{
    private readonly string _connectionString;
    private const int RETENTION_MONTHS = 3;

    public DatabaseCleanupService()
    {
        _connectionString = "Server=localhost;Database=Pos-net;Uid=root;Pwd=root;Port=3306;";
    }

    /// <summary>
    /// Run cleanup - deletes OrderWeb.net orders older than 3 months
    /// Local POS orders are NEVER deleted
    /// </summary>
    public async Task<CleanupResult> RunCleanupAsync()
    {
        var result = new CleanupResult();
        var startTime = DateTime.Now;

        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            Debug.WriteLine("🧹 Starting database cleanup...");

            // Step 1: Count what will be deleted
            var countQuery = @"
                SELECT COUNT(*) 
                FROM orders 
                WHERE created_at < DATE_SUB(NOW(), INTERVAL @months MONTH)
                AND sync_status = 'synced'
                AND cloud_order_id IS NOT NULL";

            using var countCmd = new MySqlCommand(countQuery, connection);
            countCmd.Parameters.AddWithValue("@months", RETENTION_MONTHS);
            result.OrdersDeleted = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            if (result.OrdersDeleted == 0)
            {
                Debug.WriteLine("✅ No old OrderWeb.net orders to delete");
                result.Success = true;
                return result;
            }

            Debug.WriteLine($"📊 Found {result.OrdersDeleted} OrderWeb.net orders older than {RETENTION_MONTHS} months");

            // Step 2: Get IDs of orders to delete (for logging)
            var idsQuery = @"
                SELECT id, order_number, created_at 
                FROM orders 
                WHERE created_at < DATE_SUB(NOW(), INTERVAL @months MONTH)
                AND sync_status = 'synced'
                AND cloud_order_id IS NOT NULL
                LIMIT 10";

            using var idsCmd = new MySqlCommand(idsQuery, connection);
            idsCmd.Parameters.AddWithValue("@months", RETENTION_MONTHS);
            using var reader = await idsCmd.ExecuteReaderAsync();
            
            Debug.WriteLine("📋 Sample orders to be deleted:");
            while (await reader.ReadAsync())
            {
                var orderNum = reader.GetString(1); // order_number column
                var createdAt = reader.GetDateTime(2); // created_at column
                Debug.WriteLine($"   {orderNum} - {createdAt:yyyy-MM-dd}");
            }
            await reader.CloseAsync();

            // Step 3: Delete order item addons first (foreign key constraint)
            var deleteAddonsQuery = @"
                DELETE oia FROM order_item_addons oia
                INNER JOIN order_items oi ON oia.order_item_id = oi.id
                INNER JOIN orders o ON oi.order_id = o.id
                WHERE o.created_at < DATE_SUB(NOW(), INTERVAL @months MONTH)
                AND o.sync_status = 'synced'
                AND o.cloud_order_id IS NOT NULL";

            using var deleteAddonsCmd = new MySqlCommand(deleteAddonsQuery, connection);
            deleteAddonsCmd.Parameters.AddWithValue("@months", RETENTION_MONTHS);
            result.AddonsDeleted = await deleteAddonsCmd.ExecuteNonQueryAsync();

            Debug.WriteLine($"🗑️  Deleted {result.AddonsDeleted} order addons");

            // Step 4: Delete order items
            var deleteItemsQuery = @"
                DELETE oi FROM order_items oi
                INNER JOIN orders o ON oi.order_id = o.id
                WHERE o.created_at < DATE_SUB(NOW(), INTERVAL @months MONTH)
                AND o.sync_status = 'synced'
                AND o.cloud_order_id IS NOT NULL";

            using var deleteItemsCmd = new MySqlCommand(deleteItemsQuery, connection);
            deleteItemsCmd.Parameters.AddWithValue("@months", RETENTION_MONTHS);
            result.ItemsDeleted = await deleteItemsCmd.ExecuteNonQueryAsync();

            Debug.WriteLine($"🗑️  Deleted {result.ItemsDeleted} order items");

            // Step 5: Delete orders (ONLY OrderWeb.net orders, NEVER local POS)
            var deleteOrdersQuery = @"
                DELETE FROM orders 
                WHERE created_at < DATE_SUB(NOW(), INTERVAL @months MONTH)
                AND sync_status = 'synced'
                AND cloud_order_id IS NOT NULL";

            using var deleteOrdersCmd = new MySqlCommand(deleteOrdersQuery, connection);
            deleteOrdersCmd.Parameters.AddWithValue("@months", RETENTION_MONTHS);
            var ordersDeleted = await deleteOrdersCmd.ExecuteNonQueryAsync();

            Debug.WriteLine($"🗑️  Deleted {ordersDeleted} OrderWeb.net orders");

            // Step 6: Optimize tables (reclaim space)
            await OptimizeTablesAsync(connection);

            result.Success = true;
            result.Duration = (DateTime.Now - startTime).TotalMilliseconds;

            Debug.WriteLine($"✅ Cleanup completed in {result.Duration:F0}ms");
            Debug.WriteLine($"📊 Summary: {result.OrdersDeleted} orders, {result.ItemsDeleted} items, {result.AddonsDeleted} addons deleted");
            Debug.WriteLine($"🔒 LOCAL POS ORDERS: Untouched and safe!");

            // Save cleanup timestamp
            Preferences.Set("LastCleanupDate", DateTime.Now.ToString("O"));

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            Debug.WriteLine($"❌ Cleanup error: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Create database indexes for better performance
    /// </summary>
    public async Task CreateIndexesAsync()
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            Debug.WriteLine("🔧 Creating database indexes for performance...");

            // Index for today's orders query (most common)
            await CreateIndexIfNotExistsAsync(connection, 
                "idx_orders_created_sync", 
                "orders", 
                "(created_at DESC, sync_status)");

            // Index for order number lookup
            await CreateIndexIfNotExistsAsync(connection, 
                "idx_orders_number", 
                "orders", 
                "(order_number)");

            // Index for customer search
            await CreateIndexIfNotExistsAsync(connection, 
                "idx_orders_customer", 
                "orders", 
                "(customer_name, customer_phone)");

            // Index for order items
            await CreateIndexIfNotExistsAsync(connection, 
                "idx_order_items_order_id", 
                "order_items", 
                "(order_id)");

            Debug.WriteLine("✅ Database indexes created successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ Error creating indexes: {ex.Message}");
        }
    }

    private async Task CreateIndexIfNotExistsAsync(MySqlConnection connection, string indexName, string tableName, string columns)
    {
        try
        {
            // Check if index exists
            var checkQuery = $@"
                SELECT COUNT(*) 
                FROM information_schema.STATISTICS 
                WHERE TABLE_SCHEMA = 'Pos-net' 
                AND TABLE_NAME = '{tableName}' 
                AND INDEX_NAME = '{indexName}'";

            using var checkCmd = new MySqlCommand(checkQuery, connection);
            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

            if (!exists)
            {
                var createQuery = $"CREATE INDEX {indexName} ON {tableName} {columns}";
                using var createCmd = new MySqlCommand(createQuery, connection);
                await createCmd.ExecuteNonQueryAsync();
                Debug.WriteLine($"   ✅ Created index: {indexName}");
            }
            else
            {
                Debug.WriteLine($"   ℹ️  Index exists: {indexName}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"   ⚠️ Could not create index {indexName}: {ex.Message}");
        }
    }

    private async Task OptimizeTablesAsync(MySqlConnection connection)
    {
        try
        {
            Debug.WriteLine("🔧 Optimizing tables...");

            var tables = new[] { "orders", "order_items", "order_item_addons" };
            foreach (var table in tables)
            {
                using var cmd = new MySqlCommand($"OPTIMIZE TABLE {table}", connection);
                await cmd.ExecuteNonQueryAsync();
            }

            Debug.WriteLine("✅ Tables optimized");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"⚠️ Table optimization warning: {ex.Message}");
        }
    }

    /// <summary>
    /// Get cleanup statistics
    /// </summary>
    public async Task<CleanupStats> GetCleanupStatsAsync()
    {
        var stats = new CleanupStats();

        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Count old OrderWeb.net orders
            var countQuery = @"
                SELECT COUNT(*) 
                FROM orders 
                WHERE created_at < DATE_SUB(NOW(), INTERVAL @months MONTH)
                AND sync_status = 'synced'
                AND cloud_order_id IS NOT NULL";

            using var countCmd = new MySqlCommand(countQuery, connection);
            countCmd.Parameters.AddWithValue("@months", RETENTION_MONTHS);
            stats.OldWebOrdersCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            // Count total local POS orders
            var localQuery = @"
                SELECT COUNT(*) 
                FROM orders 
                WHERE sync_status != 'synced' OR cloud_order_id IS NULL";

            using var localCmd = new MySqlCommand(localQuery, connection);
            stats.LocalPosOrdersCount = Convert.ToInt32(await localCmd.ExecuteScalarAsync());

            // Get last cleanup date
            var lastCleanup = Preferences.Get("LastCleanupDate", "");
            if (!string.IsNullOrEmpty(lastCleanup))
            {
                stats.LastCleanupDate = DateTime.Parse(lastCleanup);
            }

            stats.RetentionMonths = RETENTION_MONTHS;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting cleanup stats: {ex.Message}");
        }

        return stats;
    }
}

public class CleanupResult
{
    public bool Success { get; set; }
    public int OrdersDeleted { get; set; }
    public int ItemsDeleted { get; set; }
    public int AddonsDeleted { get; set; }
    public double Duration { get; set; }
    public string ErrorMessage { get; set; } = "";
}

public class CleanupStats
{
    public int OldWebOrdersCount { get; set; }
    public int LocalPosOrdersCount { get; set; }
    public DateTime? LastCleanupDate { get; set; }
    public int RetentionMonths { get; set; }
}
