using MySqlConnector;
using POS_in_NET.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace POS_in_NET.Services
{
    /// <summary>
    /// Service for managing table sessions and enhanced restaurant operations
    /// </summary>
    public class TableSessionService
    {
        private readonly DatabaseService _databaseService;

        public TableSessionService()
        {
            _databaseService = new DatabaseService();
        }

        /// <summary>
        /// Open a new table session (seat customers)
        /// </summary>
        public async Task<(bool success, string message)> OpenTableAsync(int tableId, int partySize, string? customerNotes = null, string? specialOccasion = null)
        {
            try
            {
                using var connection = await _databaseService.GetConnectionAsync();
                
                // Check if table is available
                var checkQuery = "SELECT Status, CurrentSessionId FROM RestaurantTables WHERE Id = @tableId";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@tableId", tableId);
                
                using var reader = await checkCommand.ExecuteReaderAsync();
                if (!reader.Read())
                {
                    return (false, "Table not found");
                }
                
                var currentStatus = reader.GetString(reader.GetOrdinal("Status"));
                if (currentStatus != "Available")
                {
                    return (false, "Table is not available");
                }
                reader.Close();
                
                // Generate session number
                var sessionNumber = await GenerateSessionNumberAsync();
                
                // Create new session
                var insertQuery = @"
                    INSERT INTO TableSessions (TableId, SessionNumber, PartySize, Status, CustomerNotes, SpecialOccasion)
                    VALUES (@tableId, @sessionNumber, @partySize, 'Occupied', @customerNotes, @specialOccasion)";
                
                using var insertCommand = new MySqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@tableId", tableId);
                insertCommand.Parameters.AddWithValue("@sessionNumber", sessionNumber);
                insertCommand.Parameters.AddWithValue("@partySize", partySize);
                insertCommand.Parameters.AddWithValue("@customerNotes", customerNotes ?? "");
                insertCommand.Parameters.AddWithValue("@specialOccasion", specialOccasion);
                
                await insertCommand.ExecuteNonQueryAsync();
                
                return (true, $"Table opened successfully for {partySize} guests (Session: {sessionNumber})");
            }
            catch (Exception ex)
            {
                return (false, $"Error opening table: {ex.Message}");
            }
        }

        /// <summary>
        /// Update session status (Occupied → Ordering → FoodServed → Payment → Cleaning → Closed)
        /// </summary>
        public async Task<(bool success, string message)> UpdateSessionStatusAsync(int sessionId, TableSessionStatus newStatus)
        {
            try
            {
                using var connection = await _databaseService.GetConnectionAsync();
                
                var updateQuery = @"
                    UPDATE TableSessions 
                    SET Status = @status, UpdatedDate = CURRENT_TIMESTAMP
                    WHERE Id = @sessionId AND IsActive = TRUE";
                
                using var command = new MySqlCommand(updateQuery, connection);
                command.Parameters.AddWithValue("@sessionId", sessionId);
                command.Parameters.AddWithValue("@status", newStatus.ToString());
                
                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected > 0)
                {
                    return (true, $"Status updated to {GetStatusDisplayName(newStatus)}");
                }
                else
                {
                    return (false, "Session not found or already closed");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error updating status: {ex.Message}");
            }
        }

        /// <summary>
        /// Close table session (customers leave)
        /// </summary>
        public async Task<(bool success, string message)> CloseTableSessionAsync(int sessionId)
        {
            try
            {
                using var connection = await _databaseService.GetConnectionAsync();
                
                var updateQuery = @"
                    UPDATE TableSessions 
                    SET Status = 'Closed', 
                        EndTime = CURRENT_TIMESTAMP,
                        IsActive = FALSE,
                        UpdatedDate = CURRENT_TIMESTAMP
                    WHERE Id = @sessionId AND IsActive = TRUE";
                
                using var command = new MySqlCommand(updateQuery, connection);
                command.Parameters.AddWithValue("@sessionId", sessionId);
                
                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected > 0)
                {
                    return (true, "Table session closed successfully");
                }
                else
                {
                    return (false, "Session not found or already closed");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error closing session: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all tables with current session information
        /// </summary>
        public async Task<List<RestaurantTable>> GetTablesWithSessionsAsync(int? floorId = null)
        {
            var tables = new List<RestaurantTable>();

            try
            {
                using var connection = await _databaseService.GetConnectionAsync();
                
                var query = @"
                    SELECT 
                        t.Id, t.TableNumber, t.FloorId, t.Capacity, t.Shape, t.Status, 
                        t.TableDesignIcon, t.CreatedDate, t.UpdatedDate, t.IsActive, t.CurrentSessionId, 
                        t.LastOccupied, t.TotalSessionsToday,
                        f.Name AS FloorName,
                        s.Id AS SessionId, s.SessionNumber, s.PartySize, s.StartTime, 
                        s.Status AS SessionStatus, s.CustomerNotes, s.SpecialOccasion,
                        s.EstimatedDuration,
                        TIMESTAMPDIFF(MINUTE, s.StartTime, NOW()) AS MinutesOccupied
                    FROM RestaurantTables t
                    INNER JOIN Floors f ON t.FloorId = f.Id
                    LEFT JOIN TableSessions s ON t.CurrentSessionId = s.Id AND s.IsActive = TRUE
                    WHERE t.IsActive = TRUE AND f.IsActive = TRUE";
                
                if (floorId.HasValue)
                {
                    query += " AND t.FloorId = @floorId";
                }
                
                query += " ORDER BY f.Name, t.TableNumber";

                using var command = new MySqlCommand(query, connection);
                if (floorId.HasValue)
                {
                    command.Parameters.AddWithValue("@floorId", floorId.Value);
                }

                using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    var table = new RestaurantTable
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        TableNumber = reader.GetString(reader.GetOrdinal("TableNumber")),
                        FloorId = reader.GetInt32(reader.GetOrdinal("FloorId")),
                        Capacity = reader.GetInt32(reader.GetOrdinal("Capacity")),
                        Shape = Enum.Parse<TableShape>(reader.GetString(reader.GetOrdinal("Shape"))),
                        Status = Enum.Parse<TableStatus>(reader.GetString(reader.GetOrdinal("Status"))),
                        TableDesignIcon = reader.IsDBNull(reader.GetOrdinal("TableDesignIcon")) ? "table_1.png" : reader.GetString(reader.GetOrdinal("TableDesignIcon")),
                        CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                        UpdatedDate = reader.GetDateTime(reader.GetOrdinal("UpdatedDate")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        CurrentSessionId = reader.IsDBNull(reader.GetOrdinal("CurrentSessionId")) ? null : reader.GetInt32(reader.GetOrdinal("CurrentSessionId")),
                        LastOccupied = reader.IsDBNull(reader.GetOrdinal("LastOccupied")) ? null : reader.GetDateTime(reader.GetOrdinal("LastOccupied")),
                        TotalSessionsToday = reader.GetInt32(reader.GetOrdinal("TotalSessionsToday")),
                        FloorName = reader.GetString(reader.GetOrdinal("FloorName"))
                    };

                    // Add session information if exists
                    if (!reader.IsDBNull(reader.GetOrdinal("SessionId")))
                    {
                        table.CurrentSession = new TableSession
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("SessionId")),
                            SessionNumber = reader.GetString(reader.GetOrdinal("SessionNumber")),
                            PartySize = reader.GetInt32(reader.GetOrdinal("PartySize")),
                            StartTime = reader.GetDateTime(reader.GetOrdinal("StartTime")),
                            Status = Enum.Parse<TableSessionStatus>(reader.GetString(reader.GetOrdinal("SessionStatus"))),
                            CustomerNotes = reader.IsDBNull(reader.GetOrdinal("CustomerNotes")) ? null : reader.GetString(reader.GetOrdinal("CustomerNotes")),
                            SpecialOccasion = reader.IsDBNull(reader.GetOrdinal("SpecialOccasion")) ? null : reader.GetString(reader.GetOrdinal("SpecialOccasion")),
                            EstimatedDuration = reader.GetInt32(reader.GetOrdinal("EstimatedDuration"))
                        };
                    }

                    tables.Add(table);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading tables with sessions: {ex.Message}");
            }

            return tables;
        }

        /// <summary>
        /// Add a note to a table session
        /// </summary>
        public async Task<(bool success, string message)> AddSessionNoteAsync(int sessionId, string note, SessionNoteType noteType, string createdBy)
        {
            try
            {
                using var connection = await _databaseService.GetConnectionAsync();
                
                var insertQuery = @"
                    INSERT INTO SessionNotes (SessionId, Note, NoteType, CreatedBy)
                    VALUES (@sessionId, @note, @noteType, @createdBy)";
                
                using var command = new MySqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@sessionId", sessionId);
                command.Parameters.AddWithValue("@note", note);
                command.Parameters.AddWithValue("@noteType", noteType.ToString());
                command.Parameters.AddWithValue("@createdBy", createdBy);
                
                await command.ExecuteNonQueryAsync();
                
                return (true, "Note added successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error adding note: {ex.Message}");
            }
        }

        /// <summary>
        /// Get session notes for a table session
        /// </summary>
        public async Task<List<SessionNote>> GetSessionNotesAsync(int sessionId)
        {
            var notes = new List<SessionNote>();

            try
            {
                using var connection = await _databaseService.GetConnectionAsync();
                
                var query = @"
                    SELECT Id, SessionId, Note, NoteType, CreatedBy, CreatedDate
                    FROM SessionNotes
                    WHERE SessionId = @sessionId
                    ORDER BY CreatedDate DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@sessionId", sessionId);

                using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    notes.Add(new SessionNote
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        SessionId = reader.GetInt32(reader.GetOrdinal("SessionId")),
                        Note = reader.GetString(reader.GetOrdinal("Note")),
                        NoteType = Enum.Parse<SessionNoteType>(reader.GetString(reader.GetOrdinal("NoteType"))),
                        CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? null : reader.GetString(reader.GetOrdinal("CreatedBy")),
                        CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate"))
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading session notes: {ex.Message}");
            }

            return notes;
        }

        /// <summary>
        /// Generate next session number (S001, S002, etc.)
        /// </summary>
        private async Task<string> GenerateSessionNumberAsync()
        {
            try
            {
                using var connection = await _databaseService.GetConnectionAsync();
                
                var query = @"
                    SELECT COUNT(*) + 1 AS NextNumber 
                    FROM TableSessions 
                    WHERE DATE(CreatedDate) = CURDATE()";
                
                using var command = new MySqlCommand(query, connection);
                var nextNumber = Convert.ToInt32(await command.ExecuteScalarAsync());
                
                return $"S{nextNumber:D3}"; // S001, S002, etc.
            }
            catch
            {
                return $"S{DateTime.Now:HHmmss}"; // Fallback to timestamp
            }
        }

        /// <summary>
        /// Get display name for session status
        /// </summary>
        private string GetStatusDisplayName(TableSessionStatus status) => status switch
        {
            TableSessionStatus.Occupied => "Just Seated",
            TableSessionStatus.Ordering => "Taking Order", 
            TableSessionStatus.FoodServed => "Dining",
            TableSessionStatus.Payment => "Ready to Pay",
            TableSessionStatus.Cleaning => "Cleaning",
            TableSessionStatus.Closed => "Closed",
            _ => status.ToString()
        };
    }
}