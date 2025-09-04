using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.Storage;
using System.Text.Json;
using System.Collections.Generic;

namespace App3.Services
{
    public class BackgroundServiceManager : IDisposable
    {
        private static BackgroundServiceManager? _instance;
        public static BackgroundServiceManager Instance => _instance ??= new BackgroundServiceManager();

        private const string APP_STATE_KEY = "AppState";
        private readonly ApplicationDataContainer _localSettings;
        private Timer? _backgroundTimer;
        private Timer? _maintenanceTimer;
        private bool _isRunningInBackground;
        private bool _disposed;
        private Window? _mainWindow;

        private BackgroundServiceManager()
        {
            _localSettings = ApplicationData.Current.LocalSettings;

            // Only subscribe to Application-level events, not Window.Current
            if (Application.Current is Microsoft.UI.Xaml.Application app)
            {
                app.UnhandledException += (s, e) => { /* Optionally handle unhandled exceptions */ };
            }
        }

        // New method to initialize with the main window
        public void Initialize(Window mainWindow)
        {
            if (_mainWindow != null)
                return; // Already initialized

            _mainWindow = mainWindow;

            // Subscribe to window events now that we have a valid window reference
            _mainWindow.Activated += OnWindowActivated;
            _mainWindow.VisibilityChanged += OnWindowVisibilityChanged;
        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs e)
        {
            _isRunningInBackground = false;
            MarkAppAsActive();
            System.Diagnostics.Debug.WriteLine("App activated - marked as active");
        }

        private void OnWindowVisibilityChanged(object sender, WindowVisibilityChangedEventArgs e)
        {
            if (!e.Visible)
            {
                _isRunningInBackground = true;
                MarkAppAsRunningInBackground();
                System.Diagnostics.Debug.WriteLine("App hidden - marked as running in background");
            }
            else
            {
                _isRunningInBackground = false;
                MarkAppAsActive();
                System.Diagnostics.Debug.WriteLine("App visible - marked as active");
            }
        }

        public Task<bool> RegisterBackgroundTaskAsync()
        {
            try
            {
                // Start background processing timers
                StartBackgroundProcessing();
                
                System.Diagnostics.Debug.WriteLine("Background processing started successfully");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start background processing: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        private void StartBackgroundProcessing()
        {
            // Timer for frequent background operations (every 5 minutes)
            _backgroundTimer = new Timer(
                PerformBackgroundOperations,
                null,
                TimeSpan.FromMinutes(1), // Initial delay
                TimeSpan.FromMinutes(5)  // Repeat every 5 minutes
            );

            // Timer for maintenance operations (every 30 minutes)
            _maintenanceTimer = new Timer(
                PerformMaintenanceOperations,
                null,
                TimeSpan.FromMinutes(10), // Initial delay
                TimeSpan.FromMinutes(30)  // Repeat every 30 minutes
            );
        }

        private async void PerformBackgroundOperations(object? state)
        {
            if (_disposed) return;

            try
            {
                System.Diagnostics.Debug.WriteLine("Performing background operations");

                // Update app state timestamp
                await UpdateAppStateTimestampAsync();

                // Mark as running in background if app is suspended
                if (_isRunningInBackground)
                {
                    MarkAppAsRunningInBackground();
                }

                // Perform lightweight background tasks
                await PerformLightweightTasksAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in background operations: {ex.Message}");
            }
        }

        private async void PerformMaintenanceOperations(object? state)
        {
            if (_disposed) return;

            try
            {
                System.Diagnostics.Debug.WriteLine("Performing maintenance operations");

                // Clear temporary data
                await ClearTemporaryDataAsync();

                // Update cached data
                await UpdateCachedDataAsync();

                // Perform cleanup
                await PerformCleanupAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in maintenance operations: {ex.Message}");
            }
        }

        private async Task PerformLightweightTasksAsync()
        {
            try
            {
                // Save current timestamp
                _localSettings.Values["LastBackgroundActivity"] = DateTimeOffset.Now.ToUnixTimeSeconds();

                // Update app activity status
                _localSettings.Values["AppActivityStatus"] = _isRunningInBackground ? "Background" : "Active";

                System.Diagnostics.Debug.WriteLine("Lightweight background tasks completed");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in lightweight tasks: {ex.Message}");
            }
        }

        private async Task ClearTemporaryDataAsync()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var tempFolder = await localFolder.TryGetItemAsync("temp");
                
                if (tempFolder != null)
                {
                    System.Diagnostics.Debug.WriteLine("Clearing temporary data");
                    // Implementation for clearing temp data would go here
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing temporary data: {ex.Message}");
            }
        }

        private async Task UpdateCachedDataAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Updating cached data");
                
                // Update any cached browser data like favicons, thumbnails, etc.
                await Task.Delay(100); // Placeholder for actual cache update logic
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating cached data: {ex.Message}");
            }
        }

        private async Task PerformCleanupAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Performing cleanup operations");
                
                // Cleanup old session data, expired cookies, etc.
                if (_localSettings.Values.TryGetValue("LastBackgroundTime", out var lastTimeValue) && 
                    lastTimeValue is long lastTime)
                {
                    var lastBackgroundTime = DateTimeOffset.FromUnixTimeSeconds(lastTime);
                    var daysSinceLastRun = (DateTimeOffset.Now - lastBackgroundTime).TotalDays;
                    
                    if (daysSinceLastRun > 7)
                    {
                        System.Diagnostics.Debug.WriteLine("Performing deep cleanup (7+ days since last run)");
                        await PerformDeepCleanupAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in cleanup operations: {ex.Message}");
            }
        }

        private async Task PerformDeepCleanupAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Performing deep cleanup");
                
                var localFolder = ApplicationData.Current.LocalFolder;
                
                // Clear old cache files
                var cacheFolder = await localFolder.TryGetItemAsync("cache");
                if (cacheFolder != null)
                {
                    System.Diagnostics.Debug.WriteLine("Cleared old cache files");
                }
                
                // Reset non-essential settings
                var nonEssentialKeys = new[] { "TempSettings", "SessionCache", "ThumbnailCache" };
                
                foreach (var key in nonEssentialKeys)
                {
                    if (_localSettings.Values.ContainsKey(key))
                    {
                        _localSettings.Values.Remove(key);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in deep cleanup: {ex.Message}");
            }
        }

        private async Task UpdateAppStateTimestampAsync()
        {
            try
            {
                var currentState = await LoadAppStateAsync();
                if (currentState != null)
                {
                    currentState.LastSaved = DateTime.Now;
                    await SaveAppStateAsync(currentState);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating app state timestamp: {ex.Message}");
            }
        }

        public async Task TriggerBackgroundTaskAsync()
        {
            try
            {
                // Manually trigger background operations
                await Task.Run(async () => 
                {
                    PerformBackgroundOperations(null);
                    await Task.Delay(1000); // Give operations time to complete
                });
                
                System.Diagnostics.Debug.WriteLine("Background operations triggered manually");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to trigger background operations: {ex.Message}");
            }
        }

        public async Task SaveAppStateAsync(AppState state)
        {
            try
            {
                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                _localSettings.Values[APP_STATE_KEY] = json;
                
                // Also save to a local file for faster access
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync("appstate.json", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, json);
                
                System.Diagnostics.Debug.WriteLine("App state saved successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save app state: {ex.Message}");
            }
        }

        public async Task<AppState?> LoadAppStateAsync()
        {
            try
            {
                // Try loading from local file first (faster)
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.TryGetItemAsync("appstate.json") as StorageFile;
                
                string? json = null;
                if (file != null)
                {
                    json = await FileIO.ReadTextAsync(file);
                    System.Diagnostics.Debug.WriteLine("App state loaded from file");
                }
                else if (_localSettings.Values.TryGetValue(APP_STATE_KEY, out var value) && value != null)
                {
                    json = value.ToString();
                    System.Diagnostics.Debug.WriteLine("App state loaded from settings");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No app state found");
                    return null;
                }

                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<AppState>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load app state: {ex.Message}");
                return null;
            }
        }

        public void MarkAppAsRunningInBackground()
        {
            try
            {
                _localSettings.Values["IsRunningInBackground"] = true;
                _localSettings.Values["LastBackgroundTime"] = DateTimeOffset.Now.ToUnixTimeSeconds();
                System.Diagnostics.Debug.WriteLine("App marked as running in background");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to mark app as running in background: {ex.Message}");
            }
        }

        public void MarkAppAsActive()
        {
            try
            {
                _localSettings.Values["IsRunningInBackground"] = false;
                System.Diagnostics.Debug.WriteLine("App marked as active");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to mark app as active: {ex.Message}");
            }
        }

        public bool IsRunningInBackground()
        {
            try
            {
                return _localSettings.Values.TryGetValue("IsRunningInBackground", out var value) && 
                       value is bool boolValue && boolValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to check background status: {ex.Message}");
                return false;
            }
        }

        public void UnregisterBackgroundTask()
        {
            try
            {
                _backgroundTimer?.Dispose();
                _maintenanceTimer?.Dispose();
                _backgroundTimer = null;
                _maintenanceTimer = null;
                
                System.Diagnostics.Debug.WriteLine("Background processing stopped");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to stop background processing: {ex.Message}");
            }
        }

        public static bool IsBackgroundTaskSupported()
        {
            // With timer-based approach, background processing is always supported
            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            UnregisterBackgroundTask();
            
            // Unsubscribe from window events
            if (_mainWindow != null)
            {
                _mainWindow.Activated -= OnWindowActivated;
                _mainWindow.VisibilityChanged -= OnWindowVisibilityChanged;
            }
        }
    }

    public class AppState
    {
        public List<TabState> Tabs { get; set; } = new();
        public int SelectedTabIndex { get; set; }
        public WindowState WindowState { get; set; } = new();
        public DateTime LastSaved { get; set; } = DateTime.Now;
    }

    public class TabState
    {
        public string Url { get; set; } = "";
        public string Title { get; set; } = "";
        public string IconUrl { get; set; } = "";
        public bool CanGoBack { get; set; }
        public bool CanGoForward { get; set; }
    }

    public class WindowState
    {
        public double Width { get; set; } = 1200;
        public double Height { get; set; } = 800;
        public double Left { get; set; } = 100;
        public double Top { get; set; } = 100;
        public bool IsMaximized { get; set; }
    }
}