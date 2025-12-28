using MySqlConnector;
using POS_in_NET.Models;
using System.Diagnostics;

namespace POS_in_NET.Services;

public class BusinessSettingsService
{
    private readonly string _connectionString;

    public BusinessSettingsService()
    {
        _connectionString = "Server=localhost;Database=Pos-net;Uid=root;Pwd=root;Port=3306;";
        EnsureBusinessInfoTableExists();
    }

    private void EnsureBusinessInfoTableExists()
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            var createTableQuery = @"
                CREATE TABLE IF NOT EXISTS business_info (
                    id INT PRIMARY KEY AUTO_INCREMENT,
                    restaurant_name VARCHAR(255) NOT NULL DEFAULT 'Restaurant POS',
                    address TEXT DEFAULT '',
                    phone_number VARCHAR(50) DEFAULT '',
                    email VARCHAR(255) DEFAULT '',
                    tax_code VARCHAR(100) DEFAULT '',
                    description TEXT DEFAULT '',
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    updated_by VARCHAR(100) DEFAULT ''
                );
            ";

            using var command = new MySqlCommand(createTableQuery, connection);
            command.ExecuteNonQuery();

            // Insert default business info if none exists
            var checkQuery = "SELECT COUNT(*) FROM business_info";
            using var checkCommand = new MySqlCommand(checkQuery, connection);
            var count = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (count == 0)
            {
                var insertQuery = @"
                    INSERT INTO business_info (restaurant_name, address, phone_number, email, description)
                    VALUES ('Restaurant POS', '', '', '', 'Premium Dining Experience')
                ";
                using var insertCommand = new MySqlCommand(insertQuery, connection);
                insertCommand.ExecuteNonQuery();
            }

            Debug.WriteLine("Business info table created/verified successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating business info table: {ex.Message}");
        }
    }

    public async Task<BusinessInfo?> GetBusinessInfoAsync()
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT id, restaurant_name, address, phone_number, email, tax_code, description, 
                       label_printer_ip, label_printer_port, label_printer_enabled,
                       updated_at, updated_by
                FROM business_info 
                ORDER BY id ASC 
                LIMIT 1
            ";

            using var command = new MySqlCommand(query, connection);
            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new BusinessInfo
                {
                    Id = Convert.ToInt32(reader["id"]),
                    RestaurantName = reader["restaurant_name"]?.ToString() ?? "",
                    Address = reader["address"]?.ToString() ?? "",
                    PhoneNumber = reader["phone_number"]?.ToString() ?? "",
                    Email = reader["email"]?.ToString() ?? "",
                    TaxCode = reader["tax_code"]?.ToString() ?? "",
                    Description = reader["description"]?.ToString() ?? "",
                    LabelPrinterIp = reader["label_printer_ip"]?.ToString(),
                    LabelPrinterPort = reader.IsDBNull(reader.GetOrdinal("label_printer_port")) ? 9100 : Convert.ToInt32(reader["label_printer_port"]),
                    LabelPrinterEnabled = reader.IsDBNull(reader.GetOrdinal("label_printer_enabled")) ? false : Convert.ToBoolean(reader["label_printer_enabled"]),
                    UpdatedAt = Convert.ToDateTime(reader["updated_at"]),
                    UpdatedBy = reader["updated_by"]?.ToString() ?? ""
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting business info: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateBusinessInfoAsync(BusinessInfo businessInfo, string updatedBy)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE business_info 
                SET restaurant_name = @restaurantName,
                    address = @address,
                    phone_number = @phoneNumber,
                    email = @email,
                    tax_code = @taxCode,
                    description = @description,
                    label_printer_ip = @labelPrinterIp,
                    label_printer_port = @labelPrinterPort,
                    label_printer_enabled = @labelPrinterEnabled,
                    updated_by = @updatedBy,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @id
            ";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@restaurantName", businessInfo.RestaurantName);
            command.Parameters.AddWithValue("@address", businessInfo.Address);
            command.Parameters.AddWithValue("@phoneNumber", businessInfo.PhoneNumber);
            command.Parameters.AddWithValue("@email", businessInfo.Email);
            command.Parameters.AddWithValue("@taxCode", businessInfo.TaxCode);
            command.Parameters.AddWithValue("@description", businessInfo.Description);
            command.Parameters.AddWithValue("@labelPrinterIp", (object?)businessInfo.LabelPrinterIp ?? DBNull.Value);
            command.Parameters.AddWithValue("@labelPrinterPort", businessInfo.LabelPrinterPort);
            command.Parameters.AddWithValue("@labelPrinterEnabled", businessInfo.LabelPrinterEnabled);
            command.Parameters.AddWithValue("@updatedBy", updatedBy);
            command.Parameters.AddWithValue("@id", businessInfo.Id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating business info: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CreateBusinessInfoAsync(BusinessInfo businessInfo, string createdBy)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                INSERT INTO business_info (restaurant_name, address, phone_number, email, tax_code, description, updated_by)
                VALUES (@restaurantName, @address, @phoneNumber, @email, @taxCode, @description, @updatedBy)
            ";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@restaurantName", businessInfo.RestaurantName);
            command.Parameters.AddWithValue("@address", businessInfo.Address);
            command.Parameters.AddWithValue("@phoneNumber", businessInfo.PhoneNumber);
            command.Parameters.AddWithValue("@email", businessInfo.Email);
            command.Parameters.AddWithValue("@taxCode", businessInfo.TaxCode);
            command.Parameters.AddWithValue("@description", businessInfo.Description);
            command.Parameters.AddWithValue("@updatedBy", createdBy);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating business info: {ex.Message}");
            return false;
        }
    }
}