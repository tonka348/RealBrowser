using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Composition.SystemBackdrops;
using WinRT;
using System.Linq;
using Microsoft.UI.Xaml.Media;

namespace App3
{
    public sealed partial class SettingsWindow : Window
    {
        private readonly ApplicationDataContainer _localSettings;
        private AppWindow? _appWindow;
        private bool _isInitialized = false;

        public SettingsWindow()
        {
            this.InitializeComponent();
            _localSettings = ApplicationData.Current.LocalSettings;
            
            SetupWindow();
            LoadSettings();
            _isInitialized = true;
        }

        private void SetupWindow()
        {
            Title = "Settings";
            
//his line:
  //          savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Documents;

            // With this line:
            // Set window size
            _appWindow = GetAppWindowForCurrentWindow();
            if (_appWindow != null)
            {
                _appWindow.Resize(new Windows.Graphics.SizeInt32(600, 700));
                
                // Center the window
                var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(_appWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                if (displayArea != null)
                {
                    var centerX = (displayArea.WorkArea.Width - 600) / 2;
                    var centerY = (displayArea.WorkArea.Height - 700) / 2;
                    _appWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
                }
            }

            // Apply Mica backdrop
            try
            {
                this.SystemBackdrop = new MicaBackdrop
                {
                    Kind = MicaKind.BaseAlt
                };
            }
            catch
            {
                // Mica not supported, use default background
            }
        }

        private AppWindow? GetAppWindowForCurrentWindow()
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }

        private void LoadSettings()
        {
            // Load homepage setting
            if (_localSettings.Values.TryGetValue("HomepageType", out var homepageType))
            {
                HomePageCombo.SelectedIndex = (homepageType.ToString()) switch
                {
                    "Blank" => 1,
                    "Custom" => 2,
                    _ => 0,
                };
            }

            // Load custom homepage URL
            if (_localSettings.Values.TryGetValue("CustomHomepage", out var customUrl))
            {
                CustomHomePageBox.Text = customUrl.ToString();
            }

            // Load dark mode setting
            if (_localSettings.Values.TryGetValue("DarkMode", out var darkMode) && darkMode is bool isDark)
            {
                DarkModeSwitch.IsOn = isDark;
            }

            // Load animations setting
            EnableAnimationsSwitch.IsOn = _localSettings.Values.TryGetValue("EnableAnimations", out var animations) ? (bool)animations : true;

            // Load swipe gestures setting
            EnableSwipeGesturesSwitch.IsOn = _localSettings.Values.TryGetValue("EnableSwipeGestures", out var swipe) ? (bool)swipe : true;

            // Load default zoom setting
            if (_localSettings.Values.TryGetValue("DefaultZoom", out var zoom) && zoom is double zoomValue)
            {
                ZoomSlider.Value = zoomValue;
            }

            // Load auto-save tabs setting
            AutoSaveTabsSwitch.IsOn = _localSettings.Values.TryGetValue("AutoSaveTabs", out var autoSave) ? (bool)autoSave : true;

            // Load downloads path
            if (_localSettings.Values.TryGetValue("DownloadsPath", out var downloadsPath))
            {
                DownloadsPathBox.Text = downloadsPath.ToString();
            }
            else
            {
                DownloadsPathBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
            }

            // Load startup setting
            if (_localSettings.Values.TryGetValue("StartupBehavior", out var startup))
            {
                StartupCombo.SelectedIndex = (startup.ToString()) switch
                {
                    "Homepage" => 1,
                    "RestoreTabs" => 2,
                    _ => 0,
                };
            }
        }

        private void SaveSettings()
        {
            // Don't save settings during initialization
            if (!_isInitialized) return;

            // Save homepage setting
            if (HomePageCombo?.SelectedItem is ComboBoxItem selectedHomepage)
            {
                _localSettings.Values["HomepageType"] = selectedHomepage.Content.ToString();
            }

            // Save custom homepage URL
            if (CustomHomePageBox != null)
                _localSettings.Values["CustomHomepage"] = CustomHomePageBox.Text;

            // Save other settings with null checks
            if (DarkModeSwitch != null)
                _localSettings.Values["DarkMode"] = DarkModeSwitch.IsOn;
            if (EnableAnimationsSwitch != null)
                _localSettings.Values["EnableAnimations"] = EnableAnimationsSwitch.IsOn;
            if (EnableSwipeGesturesSwitch != null)
                _localSettings.Values["EnableSwipeGestures"] = EnableSwipeGesturesSwitch.IsOn;
            if (ZoomSlider != null)
                _localSettings.Values["DefaultZoom"] = ZoomSlider.Value;
            if (AutoSaveTabsSwitch != null)
                _localSettings.Values["AutoSaveTabs"] = AutoSaveTabsSwitch.IsOn;
            if (DownloadsPathBox != null)
                _localSettings.Values["DownloadsPath"] = DownloadsPathBox.Text;

            // Save startup setting
            if (StartupCombo?.SelectedItem is ComboBoxItem selectedStartup)
            {
                _localSettings.Values["StartupBehavior"] = selectedStartup.Tag.ToString();
            }
        }

        private void DarkModeSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            SaveSettings();

            var newTheme = DarkModeSwitch.IsOn ? ElementTheme.Dark : ElementTheme.Light;

            // Apply theme to this window
            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = newTheme;
            }

            // You'll need to implement a way to communicate theme changes to other windows
            // Option 1: Use an event or messaging system
            // Option 2: Store theme in settings and have other windows check it
            // Option 3: Pass a reference to other windows when creating them
    
            // For now, just apply to this window. To apply to all windows, you'll need
            // to track them manually or use a different approach.
        }

        private async void ClearMostVisited_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Clear Most Visited Sites",
                Content = "Are you sure you want to clear all most visited sites data?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                XamlRoot = this.Content.XamlRoot,
                RequestedTheme = (this.Content as FrameworkElement).RequestedTheme
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var visitCountsFile = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "App3", "visitcounts.json");
                
                try
                {
                    if (System.IO.File.Exists(visitCountsFile))
                    {
                        System.IO.File.Delete(visitCountsFile);
                    }
                    
                    var successDialog = new ContentDialog
                    {
                        Title = "Success",
                        Content = "Most visited sites data has been cleared.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot,
                        RequestedTheme = (this.Content as FrameworkElement).RequestedTheme
                    };
                    await successDialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = $"Failed to clear data: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot,
                        RequestedTheme = (this.Content as FrameworkElement).RequestedTheme
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        private void EnableAnimationsSwitch_Toggled(object sender, RoutedEventArgs e) => SaveSettings();
        private void EnableSwipeGesturesSwitch_Toggled(object sender, RoutedEventArgs e) => SaveSettings();
        private void AutoSaveTabsSwitch_Toggled(object sender, RoutedEventArgs e) => SaveSettings();
        private void ZoomSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e) => SaveSettings();
        private void CustomHomePageBox_TextChanged(object sender, TextChangedEventArgs e) => SaveSettings();
        private void DownloadsPathBox_TextChanged(object sender, TextChangedEventArgs e) => SaveSettings();

        private void HomePageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveSettings();
            if (CustomHomePageBox != null)
            {
                CustomHomePageBox.Visibility = HomePageCombo.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void StartupCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => SaveSettings();

        private async void BrowseDownloadsPath_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            folderPicker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                DownloadsPathBox.Text = folder.Path;
                SaveSettings();
            }
        }

        private async void ResetAllSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Reset All Settings",
                Content = "Are you sure you want to reset all settings to their default values?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                XamlRoot = this.Content.XamlRoot,
                RequestedTheme = (this.Content as FrameworkElement).RequestedTheme
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _localSettings.Values.Clear();
                LoadSettings();
                
                var successDialog = new ContentDialog
                {
                    Title = "Settings Reset",
                    Content = "All settings have been reset to their default values.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot,
                    RequestedTheme = (this.Content as FrameworkElement).RequestedTheme
                };
                await successDialog.ShowAsync();
            }
        }

        private async void ExportSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Settings File", new System.Collections.Generic.List<string>() { ".json" });
                savePicker.SuggestedFileName = "BrowserSettings";

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    var settings = new System.Collections.Generic.Dictionary<string, object>();
                    foreach (var setting in _localSettings.Values)
                    {
                        settings[setting.Key] = setting.Value;
                    }
                    
                    var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    await Windows.Storage.FileIO.WriteTextAsync(file, json);
                    
                    var successDialog = new ContentDialog
                    {
                        Title = "Export Successful",
                        Content = "Settings have been exported successfully.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot,
                        RequestedTheme = (this.Content as FrameworkElement).RequestedTheme
                    };
                    await successDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Export Failed",
                    Content = $"Failed to export settings: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot,
                    RequestedTheme = (this.Content as FrameworkElement).RequestedTheme
                };
                await errorDialog.ShowAsync();
            }
        }

        private async void ImportSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
                openPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
                openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".json");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);

                var file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    var json = await Windows.Storage.FileIO.ReadTextAsync(file);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>>(json);
                    
                    if (settings != null)
                    {
                        _localSettings.Values.Clear();
                        foreach (var setting in settings)
                        {
                            object value = setting.Value.ValueKind switch
                            {
                                System.Text.Json.JsonValueKind.True => true,
                                System.Text.Json.JsonValueKind.False => false,
                                System.Text.Json.JsonValueKind.Number => setting.Value.GetDouble(),
                                System.Text.Json.JsonValueKind.String => setting.Value.GetString(),
                                _ => setting.Value.ToString()
                            };
                            _localSettings.Values[setting.Key] = value;
                        }
                        
                        LoadSettings();
                        
                        var successDialog = new ContentDialog
                        {
                            Title = "Import Successful",
                            Content = "Settings have been imported successfully.",
                            CloseButtonText = "OK",
                            XamlRoot = this.Content.XamlRoot,
                            RequestedTheme = (this.Content as FrameworkElement).RequestedTheme
                        };
                        await successDialog.ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Import Failed",
                    Content = $"Failed to import settings: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot,
                    RequestedTheme = (this.Content as FrameworkElement).RequestedTheme
                };
                await errorDialog.ShowAsync();
            }
        }
    }
}
