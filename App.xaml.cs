using POS_in_NET.Services;

namespace POS_in_NET;

public partial class App : Application
{
	private CloudOrderService? _cloudOrderService;
	private OrderWebDirectDatabaseService? _directDatabaseService;

	public App()
	{
		try
		{
			// Register Syncfusion license key FIRST (before InitializeComponent)
			// Updated: December 9, 2025 - Essential Studio v27.1.48 License
			Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("NDE1MDY0NUAzMjM3MmUzMDJlMzBQUWtDaHdJdXBlVTM0bmFxUVEveGZ1bkswUGJ6SXN1UExNeWtobERJK2p3PQ==");
			
			var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "pos-debug.log");
			File.AppendAllText(logPath, $"\n\n=== APP CONSTRUCTOR {DateTime.Now} ===\n");
			
			InitializeComponent();
			File.AppendAllText(logPath, "✅ InitializeComponent() completed\n");
			
			// Add global exception handler
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
			TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
			
			File.AppendAllText(logPath, "✅ App constructor completed\n");
		}
		catch (Exception ex)
		{
			// Try to log even if logging failed
			try
			{
				var logPath = "/tmp/pos-error.log";
				File.AppendAllText(logPath, $"CONSTRUCTOR ERROR: {ex.Message}\n{ex.StackTrace}\n");
			}
			catch { }
			throw;
		}
	}
	
	private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		if (e.ExceptionObject is Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"❌ UNHANDLED EXCEPTION: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
			System.Diagnostics.Debug.WriteLine($"Is Terminating: {e.IsTerminating}");
		}
	}
	
	private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		System.Diagnostics.Debug.WriteLine($"❌ UNOBSERVED TASK EXCEPTION: {e.Exception.Message}");
		foreach (var ex in e.Exception.InnerExceptions)
		{
			System.Diagnostics.Debug.WriteLine($"  - {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"    {ex.StackTrace}");
		}
		e.SetObserved(); // Prevent app from crashing
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "pos-debug.log");
		File.AppendAllText(logPath, $"\n\n=== APP START {DateTime.Now} ===\n");
		File.AppendAllText(logPath, "🚀 CreateWindow called - Starting app initialization\n");
		
		try
		{
			var appShell = new AppShell();
			File.AppendAllText(logPath, "✅ AppShell created successfully\n");
			
			// Navigate to login page on startup
			var window = new Window(appShell);
			File.AppendAllText(logPath, "✅ Window created successfully\n");
			
			// Use dispatcher to navigate immediately - maximum speed
			Dispatcher.Dispatch(async () =>
			{
				try
				{
					File.AppendAllText(logPath, "🔄 Attempting navigation to //login\n");
					// Navigate to login IMMEDIATELY - no await, no blocking
					await appShell.GoToAsync("//login");
					File.AppendAllText(logPath, "✅ Navigation to login successful\n");
				}
				catch (Exception ex)
				{
					File.AppendAllText(logPath, $"❌ Navigation error: {ex.Message}\n");
					File.AppendAllText(logPath, $"❌ Stack trace: {ex.StackTrace}\n");
					if (ex.InnerException != null)
					{
						File.AppendAllText(logPath, $"❌ Inner exception: {ex.InnerException.Message}\n");
					}
				}
			});
		
		// Initialize everything in background - completely non-blocking
		_ = Task.Run(async () =>
		{
			try
			{
				File.AppendAllText(logPath, "🔧 Starting database initialization...\n");
				// Test database connection (non-blocking)
				var authService = AuthenticationService.Instance;
				var connectionTest = await authService.TestDatabaseConnectionAsync();
				
				if (connectionTest.Success)
				{
					File.AppendAllText(logPath, "✅ " + connectionTest.Message + "\n");
					
					// Ensure default admin user exists
					var result = await authService.EnsureDefaultAdminUserAsync();
					
					if (result.Success)
					{
						File.AppendAllText(logPath, "✅ " + result.Message + "\n");
					}
					
					// Create PIN user "0000" for admin login (system initialization - no auth required)
					try
					{
						var pinResult = await authService.EnsureUserExistsAsync("Admin", "0000", "0000", Models.UserRole.Admin);
						if (pinResult.Success)
						{
							File.AppendAllText(logPath, "✅ PIN user '0000' created successfully\n");
						}
						else if (pinResult.Message.Contains("already exists"))
						{
							File.AppendAllText(logPath, "ℹ️  PIN user '0000' already exists\n");
						}
						else
						{
							File.AppendAllText(logPath, $"⚠️  PIN user creation: {pinResult.Message}\n");
						}
					}
					catch (Exception pinEx)
					{
						File.AppendAllText(logPath, $"⚠️  PIN user creation error: {pinEx.Message}\n");
					}
				}
				else
				{
					File.AppendAllText(logPath, "❌ Database: " + connectionTest.Message + "\n");
				}
			}
			catch (Exception ex)
			{
				File.AppendAllText(logPath, $"❌ Database init: {ex.Message}\n");
			}
		});
		
		// Initialize cloud services in separate background task
		_ = Task.Run(async () =>
		{
			try
			{
				File.AppendAllText(logPath, "☁️  Starting cloud services initialization...\n");
				// Delay cloud services initialization to prioritize UI
				await Task.Delay(2000); // Wait 2 seconds after app starts
				await InitializeCloudServicesAsync();
			}
			catch (Exception ex)
			{
				File.AppendAllText(logPath, $"❌ Cloud services: {ex.Message}\n");
			}
		});
		
		File.AppendAllText(logPath, "✅ Window initialization complete, returning window\n");
		return window;
		}
		catch (Exception ex)
		{
			File.AppendAllText(logPath, $"❌ FATAL ERROR in CreateWindow: {ex.Message}\n");
			File.AppendAllText(logPath, $"❌ Stack trace: {ex.StackTrace}\n");
			throw; // Re-throw to see full error
		}
	}

	private async Task InitializeCloudServicesAsync()
	{
		try
		{
			System.Diagnostics.Debug.WriteLine("========================================");
			System.Diagnostics.Debug.WriteLine("🚀 CLOUD SERVICES INITIALIZATION START");
			System.Diagnostics.Debug.WriteLine("========================================");
			
			// Get services from DI container
			var serviceProvider = Current?.Handler?.MauiContext?.Services;
			if (serviceProvider != null)
			{
				// Initialize WebSocket service FIRST (real-time orders)
				var webSocketService = serviceProvider.GetService<OrderWebWebSocketService>();
				var databaseService = serviceProvider.GetService<DatabaseService>();
				
				System.Diagnostics.Debug.WriteLine($"✓ WebSocket service: {(webSocketService != null ? "Found" : "NULL")}");
				System.Diagnostics.Debug.WriteLine($"✓ Database service: {(databaseService != null ? "Found" : "NULL")}");
				
				if (webSocketService != null && databaseService != null)
				{
					try
					{
						// Load cloud configuration
						System.Diagnostics.Debug.WriteLine("📖 Loading cloud configuration from database...");
						var config = await databaseService.GetCloudConfigurationAsync();
						
						if (config != null)
						{
							System.Diagnostics.Debug.WriteLine($"✓ Config loaded:");
							System.Diagnostics.Debug.WriteLine($"  - IsEnabled: {config.IsEnabled}");
							System.Diagnostics.Debug.WriteLine($"  - TenantSlug: {config.TenantSlug}");
							System.Diagnostics.Debug.WriteLine($"  - ApiKey: {(string.IsNullOrEmpty(config.ApiKey) ? "EMPTY" : $"{config.ApiKey.Length} chars")}");
							System.Diagnostics.Debug.WriteLine($"  - WebSocketUrl: {config.WebSocketUrl}");
							System.Diagnostics.Debug.WriteLine($"  - RestApiUrl: {config.RestApiBaseUrl}");
							System.Diagnostics.Debug.WriteLine($"");
							System.Diagnostics.Debug.WriteLine($"📡 DYNAMIC ENDPOINTS (changes when you update Settings):");
							System.Diagnostics.Debug.WriteLine($"  - WebSocket: {config.WebSocketUrl}");
							System.Diagnostics.Debug.WriteLine($"  - REST API: {config.RestApiBaseUrl}/{config.TenantSlug}/orders/pending");
							System.Diagnostics.Debug.WriteLine($"  - Restaurant: {config.TenantSlug}");
						}
						else
						{
							System.Diagnostics.Debug.WriteLine("⚠️  Config is NULL");
						}
						
						if (config != null && config.IsEnabled && !string.IsNullOrEmpty(config.TenantSlug))
						{
							System.Diagnostics.Debug.WriteLine("🔌 Auto-connecting WebSocket for real-time orders...");
							
							// Configure WebSocket with saved settings
							webSocketService.Configure(
								config.WebSocketUrl ?? "wss://orderweb.net:9011",
								config.TenantSlug,
								config.ApiKey ?? ""
							);
							
							// Subscribe to WebSocket reconnection events for catch-up sync
							webSocketService.ConnectionStatusChanged += async (sender, args) =>
							{
								if (args.IsConnected && _cloudOrderService != null)
								{
									System.Diagnostics.Debug.WriteLine("🔄 WebSocket reconnected - triggering catch-up sync...");
									var syncResult = await _cloudOrderService.SyncTodaysOrdersAsync();
									System.Diagnostics.Debug.WriteLine($"✅ Reconnection catch-up: {syncResult.Message}");
								}
							};
							
							// Connect to WebSocket
							bool connected = await webSocketService.ConnectAsync();
							
							if (connected)
							{
								System.Diagnostics.Debug.WriteLine("✅ WebSocket connected - real-time orders enabled!");
							}
							else
							{
								System.Diagnostics.Debug.WriteLine("⚠️ WebSocket connection failed - using backup polling");
							}
						}
						else
						{
							System.Diagnostics.Debug.WriteLine("ℹ️ Cloud settings not configured - WebSocket disabled");
							System.Diagnostics.Debug.WriteLine("   Please configure in Settings → Cloud Settings");
						}
					}
					catch (Exception wsEx)
					{
						System.Diagnostics.Debug.WriteLine($"⚠️ WebSocket auto-connect failed: {wsEx.Message}");
						System.Diagnostics.Debug.WriteLine($"   Stack: {wsEx.StackTrace}");
					}
				}
				
				// Initialize API polling service (backup for WebSocket)
				System.Diagnostics.Debug.WriteLine("🔄 Initializing CloudOrderService...");
				_cloudOrderService = serviceProvider.GetService<CloudOrderService>();
				if (_cloudOrderService != null)
				{
                    System.Diagnostics.Debug.WriteLine("✓ CloudOrderService found");
                    
                    // Link WebSocket to CloudOrderService for status monitoring
                    if (webSocketService != null)
                    {
                        _cloudOrderService.SetWebSocketService(webSocketService);
                    }
                    
                    // Link CloudOrderService to ReceiptService for print ACK
                    var receiptService = serviceProvider.GetService<ReceiptService>();
                    if (receiptService != null)
                    {
                        receiptService.SetCloudOrderService(_cloudOrderService);
                        System.Diagnostics.Debug.WriteLine("✓ ReceiptService linked to CloudOrderService for ACK");
                    }
                    
                    System.Diagnostics.Debug.WriteLine("========================================");
                    System.Diagnostics.Debug.WriteLine("🚀 Starting DUAL DELIVERY system");
                    System.Diagnostics.Debug.WriteLine("⚡ Primary: WebSocket (instant, 0.1-2s)");
                    System.Diagnostics.Debug.WriteLine("🔄 Backup: REST polling (every 3s)");
                    System.Diagnostics.Debug.WriteLine("✅ Print ACK: Enabled");
                    System.Diagnostics.Debug.WriteLine("🔁 ACK Retry: Every 60s");
                    System.Diagnostics.Debug.WriteLine("========================================");
                    
                    // Start polling service (backup)
                    await _cloudOrderService.StartPollingAsync();
                    System.Diagnostics.Debug.WriteLine("✅ REST polling started (backup mode)!");
                    
                    // Start ACK retry service (NEW!)
                    _cloudOrderService.StartAckRetryService();
                    System.Diagnostics.Debug.WriteLine("✅ ACK retry service started!");
                    
					// CATCH-UP SYNC: Fetch all today's orders on startup
					System.Diagnostics.Debug.WriteLine("========================================");
					System.Diagnostics.Debug.WriteLine("🔄 CATCH-UP SYNC: Fetching today's orders");
					System.Diagnostics.Debug.WriteLine("========================================");
					var syncResult = await _cloudOrderService.SyncTodaysOrdersAsync();
					if (syncResult.Success)
					{
						System.Diagnostics.Debug.WriteLine($"✅ Catch-up sync: {syncResult.Message}");
						System.Diagnostics.Debug.WriteLine($"📦 Found {syncResult.OrdersFound} orders from today");
					}
					else
					{
						System.Diagnostics.Debug.WriteLine($"⚠️ Catch-up sync failed: {syncResult.Message}");
					}
					System.Diagnostics.Debug.WriteLine("========================================");
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("❌ CloudOrderService is NULL!");
				}

				// Initialize direct database service
				_directDatabaseService = serviceProvider.GetService<OrderWebDirectDatabaseService>();
				if (_directDatabaseService != null)
				{
					await InitializeDirectDatabaseAsync();
				}
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("❌ ServiceProvider is NULL!");
			}
			
			System.Diagnostics.Debug.WriteLine("========================================");
			System.Diagnostics.Debug.WriteLine("✅ CLOUD SERVICES INITIALIZATION COMPLETE");
			System.Diagnostics.Debug.WriteLine("========================================");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"❌ Failed to initialize cloud services: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
		}
	}

	private async Task InitializeDirectDatabaseAsync()
	{
		try
		{
			// Get database service to check for saved connection settings
			var databaseService = Current?.Handler?.MauiContext?.Services?.GetService<DatabaseService>();
			if (databaseService == null) return;

			var config = await databaseService.GetCloudConfigAsync();
			
			// Check if direct database is enabled and configured
			if (config.GetValueOrDefault("direct_db_enabled", "False") == "True")
			{
				var host = config.GetValueOrDefault("db_host", "");
				var database = config.GetValueOrDefault("db_database", "");
				var username = config.GetValueOrDefault("db_username", "");
				var password = config.GetValueOrDefault("db_password", "");
				var portText = config.GetValueOrDefault("db_port", "3306");

				if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(database) && 
				    !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password) &&
				    int.TryParse(portText, out int port))
				{
					System.Diagnostics.Debug.WriteLine("🚀 Configuring direct database connection...");
					
					var success = await _directDatabaseService!.ConfigureDatabaseConnectionAsync(host, database, username, password, port);
					
					if (success)
					{
						await _directDatabaseService.StartRealTimeMonitoringAsync();
						System.Diagnostics.Debug.WriteLine("✅ Direct database connection established - 0.5s order delivery active!");
					}
					else
					{
						System.Diagnostics.Debug.WriteLine("❌ Direct database connection failed");
					}
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("⚠️ Direct database enabled but configuration incomplete");
				}
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("ℹ️ Direct database connection disabled");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"❌ Failed to initialize direct database: {ex.Message}");
		}
	}

	protected override void CleanUp()
	{
		// Stop cloud services when app is closing
		_cloudOrderService?.StopPolling();
		_directDatabaseService?.StopRealTimeMonitoring();
		base.CleanUp();
	}
}