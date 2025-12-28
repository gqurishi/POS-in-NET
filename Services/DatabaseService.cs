using MySqlConnector;
using System.Data;
using POS_in_NET.Models;

namespace POS_in_NET.Services;

/// <summary>
/// Database service using MariaDB/MySQL ONLY
/// This application uses MariaDB as the primary database
/// Host: localhost, Port: 3306, Database: Pos-net
/// </summary>
public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        // MariaDB/MySQL database connection configuration
        var host = "localhost";
        var user = "root";
        var password = "root";
        var database = "Pos-net"; // USE SAME DATABASE AS EVERYTHING ELSE!
        var port = "3306";
        
        // Add connection timeout to prevent hanging
        _connectionString = $"Server={host};Database={database};Uid={user};Pwd={password};Port={port};Connection Timeout=5;";
        
        // DO NOT initialize database in constructor - it blocks app startup!
        // Initialize will be called separately when needed
        // _ = InitializeDatabaseAsync(); // REMOVED - was blocking!
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            
            // Add 5-second timeout
            var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
            await connection.OpenAsync(cts.Token);
            
            // Test with a simple query
            using var command = new MySqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync();
            
            return result != null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database connection failed: {ex.Message}");
            return false;
        }
    }

    public async Task<string> GetConnectionStatusAsync()
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new MySqlCommand("SELECT VERSION()", connection);
            var version = await command.ExecuteScalarAsync();
            
            return $"Connected to MariaDB/MySQL version: {version}";
        }
        catch (Exception ex)
        {
            return $"Connection failed: {ex.Message}";
        }
    }

    public async Task<MySqlConnection> GetConnectionAsync()
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task<bool> InitializeDatabaseAsync()
    {
        try
        {
            // Create database if not exists
            await CreateDatabaseIfNotExistsAsync();
            
            // Create tables
            return await CreateTablesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database initialization failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CreateDatabaseIfNotExistsAsync()
    {
        try
        {
            // Connect without specifying database to create it if needed
            var connectionStringWithoutDb = "Server=localhost;Uid=root;Pwd=root;Port=3306;Connection Timeout=5;";
            
            using var connection = new MySqlConnection(connectionStringWithoutDb);
            await connection.OpenAsync();
            
            using var command = new MySqlCommand("CREATE DATABASE IF NOT EXISTS `Pos-net` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci", connection);
            await command.ExecuteNonQueryAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database creation failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CreateTablesAsync()
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Users table
            var createUsersTable = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    username VARCHAR(50) UNIQUE NOT NULL,
                    password_hash VARCHAR(255) NOT NULL,
                    role ENUM('admin', 'manager', 'user') DEFAULT 'user',
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                ) ENGINE=InnoDB";

            // Orders table
            var createOrdersTable = @"
                CREATE TABLE IF NOT EXISTS orders (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    order_id VARCHAR(100) UNIQUE NOT NULL,
                    customer_name VARCHAR(100),
                    customer_phone VARCHAR(20),
                    customer_address TEXT,
                    total_amount DECIMAL(10,2),
                    status ENUM('new', 'kitchen', 'preparing', 'ready', 'delivering', 'completed') DEFAULT 'new',
                    order_data JSON,
                    sync_status ENUM('synced', 'pending', 'failed') DEFAULT 'pending',
                    kitchen_time TIMESTAMP NULL,
                    preparing_time TIMESTAMP NULL,
                    ready_time TIMESTAMP NULL,
                    delivering_time TIMESTAMP NULL,
                    completed_time TIMESTAMP NULL,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                ) ENGINE=InnoDB";

            // Order items table
            var createOrderItemsTable = @"
                CREATE TABLE IF NOT EXISTS order_items (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    order_id INT,
                    item_name VARCHAR(100) NOT NULL,
                    quantity INT NOT NULL,
                    unit_price DECIMAL(10,2) NOT NULL,
                    special_instructions TEXT,
                    FOREIGN KEY (order_id) REFERENCES orders(id) ON DELETE CASCADE
                ) ENGINE=InnoDB";

            // Settings table
            var createSettingsTable = @"
                CREATE TABLE IF NOT EXISTS settings (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    setting_key VARCHAR(100) UNIQUE NOT NULL,
                    setting_value TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                ) ENGINE=InnoDB";

            // Create cloud configuration table
            var createCloudConfigTable = @"
                CREATE TABLE IF NOT EXISTS cloud_config (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    api_base_url VARCHAR(500) NOT NULL DEFAULT 'https://orderweb.net/api',
                    tenant_slug VARCHAR(255) NOT NULL DEFAULT '',
                    api_key VARCHAR(500) NOT NULL DEFAULT '',
                    websocket_url VARCHAR(500) NOT NULL DEFAULT '',
                    connection_timeout INT DEFAULT 30,
                    polling_interval_seconds INT DEFAULT 30,
                    max_retry_attempts INT DEFAULT 3,
                    is_enabled BOOLEAN DEFAULT FALSE,
                    is_api_tested BOOLEAN DEFAULT FALSE,
                    is_websocket_tested BOOLEAN DEFAULT FALSE,
                    api_test_result TEXT,
                    websocket_test_result TEXT,
                    last_api_test TIMESTAMP NULL,
                    last_websocket_test TIMESTAMP NULL,
                    auto_print_enabled BOOLEAN DEFAULT TRUE,
                    notifications_enabled BOOLEAN DEFAULT TRUE,
                    last_sync TIMESTAMP NULL,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    cloud_url VARCHAR(500) DEFAULT 'https://orderweb.net/api/pos'
                ) ENGINE=InnoDB";

            // Execute table creation commands
            using var command1 = new MySqlCommand(createUsersTable, connection);
            await command1.ExecuteNonQueryAsync();

            using var command2 = new MySqlCommand(createOrdersTable, connection);
            await command2.ExecuteNonQueryAsync();

            using var command3 = new MySqlCommand(createOrderItemsTable, connection);
            await command3.ExecuteNonQueryAsync();

            using var command4 = new MySqlCommand(createSettingsTable, connection);
            await command4.ExecuteNonQueryAsync();

            using var command5 = new MySqlCommand(createCloudConfigTable, connection);
            await command5.ExecuteNonQueryAsync();

            // Add new columns for direct database connection and OrderWeb.net integration if they don't exist
            var alterCloudConfigTable = @"
                ALTER TABLE cloud_config 
                ADD COLUMN IF NOT EXISTS db_host VARCHAR(255) DEFAULT '',
                ADD COLUMN IF NOT EXISTS db_name VARCHAR(255) DEFAULT '',
                ADD COLUMN IF NOT EXISTS db_username VARCHAR(255) DEFAULT '',
                ADD COLUMN IF NOT EXISTS db_password VARCHAR(500) DEFAULT '',
                ADD COLUMN IF NOT EXISTS db_port INT DEFAULT 3306,
                ADD COLUMN IF NOT EXISTS connection_type VARCHAR(50) DEFAULT 'api_polling',
                ADD COLUMN IF NOT EXISTS connection_string TEXT DEFAULT '',
                ADD COLUMN IF NOT EXISTS orderweb_enabled BOOLEAN DEFAULT FALSE,
                ADD COLUMN IF NOT EXISTS orderweb_connection_string TEXT DEFAULT '',
                ADD COLUMN IF NOT EXISTS restaurant_slug VARCHAR(255) DEFAULT '',
                ADD COLUMN IF NOT EXISTS direct_db_enabled BOOLEAN DEFAULT FALSE";

            try 
            {
                using var alterCommand = new MySqlCommand(alterCloudConfigTable, connection);
                await alterCommand.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex) when (ex.Number == 1060) // Duplicate column name
            {
                // Columns already exist, ignore
                System.Diagnostics.Debug.WriteLine("Cloud config columns already exist");
            }

            // Add name column to users table if it doesn't exist
            var alterUsersTable = @"
                ALTER TABLE users 
                ADD COLUMN IF NOT EXISTS name VARCHAR(100) NOT NULL DEFAULT ''";

            try 
            {
                using var alterUsersCommand = new MySqlCommand(alterUsersTable, connection);
                await alterUsersCommand.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex) when (ex.Number == 1060) // Duplicate column name
            {
                // Column already exists, ignore
                System.Diagnostics.Debug.WriteLine("Users name column already exists");
            }

            // Create default admin user if not exists
            await CreateDefaultAdminUserAsync(connection);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Table creation failed: {ex.Message}");
            return false;
        }
    }

    private async Task CreateDefaultAdminUserAsync(MySqlConnection connection)
    {
        try
        {
            // Check if admin user exists
            using var checkCommand = new MySqlCommand("SELECT COUNT(*) FROM users WHERE username = 'admin'", connection);
            var userCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

            if (userCount == 0)
            {
                // Create default admin user (password: admin123)
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword("admin123");
                using var insertCommand = new MySqlCommand(
                    "INSERT INTO users (name, username, password_hash, role) VALUES (@name, @username, @password, @role)", 
                    connection);
                
                insertCommand.Parameters.AddWithValue("@name", "Administrator");
                insertCommand.Parameters.AddWithValue("@username", "admin");
                insertCommand.Parameters.AddWithValue("@password", hashedPassword);
                insertCommand.Parameters.AddWithValue("@role", "admin");
                
                await insertCommand.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Default admin user creation failed: {ex.Message}");
        }
    }

    public string GetConnectionString()
    {
        return _connectionString;
    }

    public async Task UpdateOrderStatusAsync(int orderId, string status)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new MySqlCommand(
                "UPDATE orders SET status = @status, updated_at = NOW() WHERE id = @orderId", 
                connection);
            
            command.Parameters.AddWithValue("@status", status);
            command.Parameters.AddWithValue("@orderId", orderId);
            
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update order status: {ex.Message}");
            throw;
        }
    }

    // Cloud Configuration Management
    public async Task<Dictionary<string, string>> GetCloudConfigAsync()
    {
        var config = new Dictionary<string, string>();
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new MySqlCommand("SELECT * FROM cloud_config LIMIT 1", connection);
            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                config["tenant_slug"] = reader.GetString(reader.GetOrdinal("tenant_slug"));
                config["api_key"] = reader.GetString(reader.GetOrdinal("api_key"));
                config["cloud_url"] = reader.GetString(reader.GetOrdinal("cloud_url"));
                config["is_enabled"] = reader.GetBoolean(reader.GetOrdinal("is_enabled")).ToString();
                config["polling_interval_seconds"] = reader.GetInt32(reader.GetOrdinal("polling_interval_seconds")).ToString();
                config["auto_print_enabled"] = reader.GetBoolean(reader.GetOrdinal("auto_print_enabled")).ToString();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get cloud config: {ex.Message}");
        }
        return config;
    }

    public async Task<bool> SaveCloudConfigAsync(string tenantSlug, string apiKey, string cloudUrl, 
        bool isEnabled, int pollingInterval, bool autoPrint)
    {
        return await SaveCloudConfigAsync(tenantSlug, apiKey, cloudUrl, isEnabled, pollingInterval, autoPrint,
            "", "", "", "", 0, "api_polling");
    }

    public async Task<bool> SaveCloudConfigAsync(string tenantSlug, string apiKey, string cloudUrl, 
        bool isEnabled, int pollingInterval, bool autoPrint, string dbHost, string dbName, 
        string dbUsername, string dbPassword, int dbPort, string connectionType)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            
            // Check if config exists
            using var checkCommand = new MySqlCommand("SELECT COUNT(*) FROM cloud_config", connection);
            var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            
            var sql = count > 0 
                ? @"UPDATE cloud_config SET tenant_slug = @tenantSlug, api_key = @apiKey, 
                    cloud_url = @cloudUrl, is_enabled = @isEnabled, polling_interval_seconds = @pollingInterval,
                    auto_print_enabled = @autoPrint, db_host = @dbHost, db_name = @dbName, 
                    db_username = @dbUsername, db_password = @dbPassword, db_port = @dbPort,
                    connection_type = @connectionType, updated_at = CURRENT_TIMESTAMP"
                : @"INSERT INTO cloud_config (tenant_slug, api_key, cloud_url, is_enabled, 
                    polling_interval_seconds, auto_print_enabled, db_host, db_name, db_username, 
                    db_password, db_port, connection_type) 
                    VALUES (@tenantSlug, @apiKey, @cloudUrl, @isEnabled, @pollingInterval, @autoPrint,
                    @dbHost, @dbName, @dbUsername, @dbPassword, @dbPort, @connectionType)";
            
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@tenantSlug", tenantSlug);
            command.Parameters.AddWithValue("@apiKey", apiKey);
            command.Parameters.AddWithValue("@cloudUrl", cloudUrl);
            command.Parameters.AddWithValue("@isEnabled", isEnabled);
            command.Parameters.AddWithValue("@pollingInterval", pollingInterval);
            command.Parameters.AddWithValue("@autoPrint", autoPrint);
            command.Parameters.AddWithValue("@dbHost", dbHost ?? "");
            command.Parameters.AddWithValue("@dbName", dbName ?? "");
            command.Parameters.AddWithValue("@dbUsername", dbUsername ?? "");
            command.Parameters.AddWithValue("@dbPassword", dbPassword ?? "");
            command.Parameters.AddWithValue("@dbPort", dbPort);
            command.Parameters.AddWithValue("@connectionType", connectionType ?? "api_polling");
            
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save cloud config: {ex.Message}");
            return false;
        }
    }

    // NEW: Cloud Configuration CRUD methods for CloudConfiguration model
    public async Task<CloudConfiguration?> GetCloudConfigurationAsync()
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new MySqlCommand("SELECT * FROM cloud_config LIMIT 1", connection);
            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return new CloudConfiguration
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    ApiBaseUrl = reader.GetString(reader.GetOrdinal("api_base_url")),
                    TenantSlug = reader.GetString(reader.GetOrdinal("tenant_slug")),
                    ApiKey = reader.GetString(reader.GetOrdinal("api_key")),
                    WebSocketUrl = reader.GetString(reader.GetOrdinal("websocket_url")),
                    ConnectionTimeout = reader.GetInt32(reader.GetOrdinal("connection_timeout")),
                    PollingIntervalSeconds = reader.GetInt32(reader.GetOrdinal("polling_interval_seconds")),
                    MaxRetryAttempts = reader.GetInt32(reader.GetOrdinal("max_retry_attempts")),
                    IsEnabled = reader.GetBoolean(reader.GetOrdinal("is_enabled")),
                    IsApiTested = reader.GetBoolean(reader.GetOrdinal("is_api_tested")),
                    IsWebSocketTested = reader.GetBoolean(reader.GetOrdinal("is_websocket_tested")),
                    ApiTestResult = reader.IsDBNull("api_test_result") ? null : reader.GetString(reader.GetOrdinal("api_test_result")),
                    WebSocketTestResult = reader.IsDBNull("websocket_test_result") ? null : reader.GetString(reader.GetOrdinal("websocket_test_result")),
                    LastApiTest = reader.IsDBNull("last_api_test") ? null : reader.GetDateTime(reader.GetOrdinal("last_api_test")),
                    LastWebSocketTest = reader.IsDBNull("last_websocket_test") ? null : reader.GetDateTime(reader.GetOrdinal("last_websocket_test")),
                    AutoPrintEnabled = reader.GetBoolean(reader.GetOrdinal("auto_print_enabled")),
                    NotificationsEnabled = reader.GetBoolean(reader.GetOrdinal("notifications_enabled")),
                    LastSync = reader.IsDBNull("last_sync") ? null : reader.GetDateTime(reader.GetOrdinal("last_sync")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                };
            }
            
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get cloud configuration: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> SaveCloudConfigurationAsync(CloudConfiguration config)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔧 SaveCloudConfigurationAsync starting...");
            using var connection = await GetConnectionAsync();
            System.Diagnostics.Debug.WriteLine($"✅ Database connection established");
            
            // Check if config exists
            using var checkCommand = new MySqlCommand("SELECT COUNT(*) FROM cloud_config", connection);
            var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
            System.Diagnostics.Debug.WriteLine($"📊 Existing config count: {count}");
            
            // Use RestApiBaseUrl if available, fallback to ApiBaseUrl
            var apiBaseUrl = !string.IsNullOrEmpty(config.RestApiBaseUrl) 
                ? config.RestApiBaseUrl 
                : config.ApiBaseUrl;
            
            var sql = count > 0 
                ? @"UPDATE cloud_config SET 
                    api_base_url = @apiBaseUrl,
                    tenant_slug = @tenantSlug,
                    api_key = @apiKey,
                    websocket_url = @websocketUrl,
                    connection_timeout = @connectionTimeout,
                    polling_interval_seconds = @pollingInterval,
                    max_retry_attempts = @maxRetryAttempts,
                    is_enabled = @isEnabled,
                    is_api_tested = @isApiTested,
                    is_websocket_tested = @isWebSocketTested,
                    api_test_result = @apiTestResult,
                    websocket_test_result = @websocketTestResult,
                    last_api_test = @lastApiTest,
                    last_websocket_test = @lastWebSocketTest,
                    auto_print_enabled = @autoPrint,
                    notifications_enabled = @notificationsEnabled,
                    updated_at = CURRENT_TIMESTAMP
                    WHERE id = (SELECT MIN(id) FROM (SELECT id FROM cloud_config) as temp)"
                : @"INSERT INTO cloud_config (
                    api_base_url, tenant_slug, api_key, websocket_url,
                    connection_timeout, polling_interval_seconds, max_retry_attempts,
                    is_enabled, is_api_tested, is_websocket_tested,
                    api_test_result, websocket_test_result,
                    last_api_test, last_websocket_test,
                    auto_print_enabled, notifications_enabled
                    ) VALUES (
                    @apiBaseUrl, @tenantSlug, @apiKey, @websocketUrl,
                    @connectionTimeout, @pollingInterval, @maxRetryAttempts,
                    @isEnabled, @isApiTested, @isWebSocketTested,
                    @apiTestResult, @websocketTestResult,
                    @lastApiTest, @lastWebSocketTest,
                    @autoPrint, @notificationsEnabled
                    )";
            
            System.Diagnostics.Debug.WriteLine($"📝 SQL: {(count > 0 ? "UPDATE" : "INSERT")}");
            
            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@apiBaseUrl", apiBaseUrl);
            command.Parameters.AddWithValue("@tenantSlug", config.TenantSlug);
            command.Parameters.AddWithValue("@apiKey", config.ApiKey);
            command.Parameters.AddWithValue("@websocketUrl", config.WebSocketUrl ?? "");
            command.Parameters.AddWithValue("@connectionTimeout", config.ConnectionTimeout);
            command.Parameters.AddWithValue("@pollingInterval", config.PollingIntervalSeconds);
            command.Parameters.AddWithValue("@maxRetryAttempts", config.MaxRetryAttempts);
            command.Parameters.AddWithValue("@isEnabled", config.IsEnabled);
            command.Parameters.AddWithValue("@isApiTested", config.IsApiTested);
            command.Parameters.AddWithValue("@isWebSocketTested", config.IsWebSocketTested);
            command.Parameters.AddWithValue("@apiTestResult", (object?)config.ApiTestResult ?? DBNull.Value);
            command.Parameters.AddWithValue("@websocketTestResult", (object?)config.WebSocketTestResult ?? DBNull.Value);
            command.Parameters.AddWithValue("@lastApiTest", (object?)config.LastApiTest ?? DBNull.Value);
            command.Parameters.AddWithValue("@lastWebSocketTest", (object?)config.LastWebSocketTest ?? DBNull.Value);
            command.Parameters.AddWithValue("@autoPrint", config.AutoPrintEnabled);
            command.Parameters.AddWithValue("@notificationsEnabled", config.NotificationsEnabled);
            
            System.Diagnostics.Debug.WriteLine($"💾 Executing database command...");
            var rowsAffected = await command.ExecuteNonQueryAsync();
            System.Diagnostics.Debug.WriteLine($"✅ Rows affected: {rowsAffected}");
            
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to save cloud configuration: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<bool> UpdateConnectionTestResultsAsync(bool isApiTested, string apiResult, bool isWebSocketTested, string websocketResult)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new MySqlCommand(@"
                UPDATE cloud_config SET 
                    is_api_tested = @isApiTested,
                    api_test_result = @apiResult,
                    last_api_test = @lastApiTest,
                    is_websocket_tested = @isWebSocketTested,
                    websocket_test_result = @websocketResult,
                    last_websocket_test = @lastWebSocketTest,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = 1", connection);
            
            command.Parameters.AddWithValue("@isApiTested", isApiTested);
            command.Parameters.AddWithValue("@apiResult", apiResult);
            command.Parameters.AddWithValue("@lastApiTest", DateTime.UtcNow);
            command.Parameters.AddWithValue("@isWebSocketTested", isWebSocketTested);
            command.Parameters.AddWithValue("@websocketResult", websocketResult);
            command.Parameters.AddWithValue("@lastWebSocketTest", DateTime.UtcNow);
            
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update test results: {ex.Message}");
            return false;
        }
    }
}
