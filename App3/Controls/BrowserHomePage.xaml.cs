using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using Windows.System;
using System.Threading.Tasks;
using System.IO;
using Windows.Storage.Streams;
using Microsoft.UI;

namespace App3.Controls
{
    public sealed partial class BrowserHomePage : UserControl
    {
        public BrowserHomePage()
        {
            InitializeComponent();
            
            // Set Bing image as wallpaper on startup
            _ = SetBingWallpaper();
            
            // Other initialization code...
        }

        // Mark the event as nullable to resolve CS8618 and suppress CS0067 warning
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "CS0067", Justification = "Event reserved for future use")]
        public event EventHandler<string>? SearchRequested;

        public void FocusSearchBox()
        {
            this.Focus(FocusState.Programmatic);
        }

        private void SearchBox_QuerySubmitted(object sender, AutoSuggestBoxQuerySubmittedEventArgs e)
        {
            Debug.WriteLine($"QuerySubmitted: {e.QueryText}");
            SearchRequested?.Invoke(this, e.QueryText);
        }

        private async void SearchBox_TextChanged(object sender, AutoSuggestBoxTextChangedEventArgs e)
        {
            if (e.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var autoSuggestBox = sender as AutoSuggestBox;
                if (autoSuggestBox == null || string.IsNullOrWhiteSpace(autoSuggestBox.Text))
                {
                    if (autoSuggestBox != null)
                        autoSuggestBox.ItemsSource = null;
                    return;
                }

                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        string query = Uri.EscapeDataString(autoSuggestBox.Text);
                        string url = $"https://suggestqueries.google.com/complete/search?client=chrome&q={query}";
                        var response = await httpClient.GetStringAsync(url);
                        using (JsonDocument doc = JsonDocument.Parse(response))
                        {
                            var suggestions = new List<string>();
                            // The returned JSON structure is: [query, [suggestion1, suggestion2, ...]]
                            var suggestionArray = doc.RootElement[1];
                            foreach (var suggestion in suggestionArray.EnumerateArray())
                            {
                                suggestions.Add(suggestion.GetString() ?? string.Empty);
                            }
                            autoSuggestBox.ItemsSource = suggestions;
                        }
                    }
                }
                catch (Exception)
                {
                    // Optionally handle exceptions (i.e. logging)
                }
            }
        }

        private void SearchBox_SuggestionChosen(object sender, AutoSuggestBoxSuggestionChosenEventArgs e)
        {
            // Handle the suggestion chosen event here if needed
        }

        private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                Debug.WriteLine("Enter key pressed in SearchBox_KeyDown");
            }
        }

        private void QuickLinkButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement logic for quick link button click
        }

        // Modified SetBingWallpaper method to correctly set the Grid background
        private async Task SetBingWallpaper()
        {
            try
            {
                // Debug to verify method is called
                Debug.WriteLine("Starting to fetch Bing wallpaper...");
                
                // URL to fetch Bing's image of the day
                string bingUrl = "https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=en-US";
                
                using (var client = new HttpClient())
                {
                    // Add a user agent to avoid potential request blocking
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                    
                    // Get the Bing image metadata
                    Debug.WriteLine("Fetching Bing image metadata...");
                    var response = await client.GetStringAsync(bingUrl);
                    Debug.WriteLine($"Response received, length: {response.Length}");
                    
                    // Parse the JSON response
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(response);
                    var imageElement = jsonDoc.RootElement.GetProperty("images")[0];
                    var urlPath = imageElement.GetProperty("url").GetString();
                    var imageUrl = "https://www.bing.com" + urlPath;
                    Debug.WriteLine($"Image URL: {imageUrl}");
                    
                    // Find the root Grid in the XAML
                    var rootGrid = this.Content as Grid;
                    if (rootGrid == null)
                    {
                        Debug.WriteLine("ERROR: Could not find the root Grid!");
                        return;
                    }
                    
                    // Create image brush for the background with a simpler approach
                    var imageBrush = new ImageBrush();
                    var bingImage = new BitmapImage();
                    
                    // Set up loading event to debug image loading issues
                    bingImage.ImageOpened += (s, e) => Debug.WriteLine("Image loaded successfully!");
                    bingImage.ImageFailed += (s, e) => Debug.WriteLine($"Image failed to load: {e.ErrorMessage}");
                    
                    // Set the source URI
                    bingImage.UriSource = new Uri(imageUrl);
                    
                    // Configure the brush
                    imageBrush.ImageSource = bingImage;
                    imageBrush.Stretch = Stretch.UniformToFill;
                    imageBrush.Opacity = 0.8; // Semi-transparent to ensure content visibility
                    
                    // Apply the background to the root Grid
                    rootGrid.Background = imageBrush;
                    Debug.WriteLine("Wallpaper applied to Grid");
                }
            }
            catch (HttpRequestException httpEx)
            {
                Debug.WriteLine($"HTTP error fetching wallpaper: {httpEx.Message}");
                ApplyDefaultBackground();
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine($"JSON parsing error: {jsonEx.Message}");
                ApplyDefaultBackground();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting wallpaper: {ex.GetType().Name} - {ex.Message}");
                ApplyDefaultBackground();
            }
        }

        // Add helper method for fallback background
        private void ApplyDefaultBackground()
        {
            try
            {
                var rootGrid = this.Content as Grid;
                if (rootGrid != null)
                {
                    // Create a gradient background as fallback
                    var gradientBrush = new LinearGradientBrush
                    {
                        StartPoint = new Windows.Foundation.Point(0, 0),
                        EndPoint = new Windows.Foundation.Point(1, 1)
                    };
                    gradientBrush.GradientStops.Add(new GradientStop { Color = Colors.LightSteelBlue, Offset = 0.0 });
                    gradientBrush.GradientStops.Add(new GradientStop { Color = Colors.SlateBlue, Offset = 1.0 });
                    
                    rootGrid.Background = gradientBrush;
                    Debug.WriteLine("Applied fallback gradient background");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying fallback background: {ex.Message}");
            }
        }

        private void RefreshWallpaperButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SetBingWallpaper();
        }
    }
}