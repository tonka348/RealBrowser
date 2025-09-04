using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Windows.System;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.UI.ViewManagement;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Microsoft.UI.Composition;
using WinRT;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.System.Power;
using System.Collections.ObjectModel;

using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Composition.SystemBackdrops;
using System.Runtime.InteropServices;
using Windows.UI;




namespace App3


{
    public static class VisualTreeExtensions
    {
        // Add this field to your MainWindow class to fix CS0103
        
        public static T? FindDescendant<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                    return tChild;
                var result = FindDescendant<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        // Add this method to the VisualTreeExtensions class
        public static T? FindDescendant<T>(this DependencyObject parent, Func<T, bool> condition) where T : DependencyObject
        {
            if (parent == null) return null;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild && condition(tChild))
                    return tChild;

                var result = FindDescendant<T>(child, condition);
                if (result != null)
                    return result;
            }

            return null;
        }
        // Add this method to your MainWindow class to fix CS0103
       
        // Add this method to your MainWindow class to fix CS0103
       
        // Add this method to your MainWindow class to fix CS0103
    
        
       
        
    }
    public sealed partial class MainWindow : Window
    {
        private readonly Dictionary<TabViewItem, Microsoft.UI.Xaml.Controls.WebView2> _tabWebViews = new();
        private int _tabCounter = 1;
        private AutoSuggestBox? _searchBox;
        private Grid? _searchContainer;
        private Button? NewTabButton;
        private AppWindow? _appWindow;
        
        private const string HomepagePath = "Assets\\HomePage.html";
        private const string HammerJsMinified = @"!function(a,b) { /* ... full minified Hammer.js code here ... */ }(window,document);";
        private const string HammerJsGestureScript = @"console.log('Hammer.js gesture script starting...'); /* ... */";
        private const string SmoothScrollCssScript = @"
(function() {
  var style = document.createElement('style');
  style.textContent = `
    html, body, [style*='overflow'], [class*='scroll'], [class*='Scroll'] {
      scroll-behavior: smooth !important;
    }
  `;
  document.head.appendChild(style);
})();

";

        private bool _isPwaInstallAvailable = false;
        private double _tabSwipeStartX;
        private FrameworkElement? CloseIconLight;
        private FrameworkElement? CloseIconDark;
        private readonly UISettings _uiSettings = new UISettings();
        private static readonly string GoogleApiKey = "AIzaSyCx3FRoAwKLeidHYpOMzYJ85HKR9GKz0LY";
        private System.Threading.CancellationTokenSource? _searchDebounceCts;
        private bool _isWindowClosed = false;
        private bool _isImmersive = false;

       
       


        public ElementTheme ActualTheme { get; private set; }
        public ElementTheme RequestedTheme
        {
            get => ActualTheme;
            set
            {
                if (ActualTheme != value)
                {
                    ActualTheme = value;
                }
            }
        }

        private void ResizeTabs()
        {
            AdjustTabWidths();
        }

        // Add this method to your MainWindow class to fix CS1061
        private void BrowserTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BrowserTabView.SelectedItem is TabViewItem selectedTab &&
                _tabWebViews.TryGetValue(selectedTab, out var webView))
            {
                // Update content area with the selected tab's WebView
                ContentGrid.Children.Clear();
                ContentGrid.Children.Add(webView);
                
                // Focus the tab and its content to ensure it responds to the first click
                selectedTab.Focus(FocusState.Programmatic);
                webView.Focus(FocusState.Programmatic);
                
                // Update address bar with the current URL
                if (webView.CoreWebView2 != null && SearchBox != null)
                {
                    var url = webView.CoreWebView2.Source;
                    if (!url.Replace('/', '\\').EndsWith(HomepagePath, StringComparison.OrdinalIgnoreCase))
                    {
                        SearchBox.Text = url;
                    }
                    else
                    {
                        SearchBox.Text = string.Empty;
                    }
                }
                
                // Update navigation buttons state
                UpdateNavigationButtons();
                
                // Apply styling to the selected tab with a slight delay to ensure UI updates
                DispatcherQueue.TryEnqueue(() => {
                    ApplyAcrylicToSelectedTab(selectedTab);
                });
                
                // Ensure proper tab sizing
                ResizeTabs();
                
                // Force UI update
                selectedTab.UpdateLayout();
            }
        }

        public TypedEventHandler<CoreWebView2, CoreWebView2NewWindowRequestedEventArgs>? WebView2_NewWindowRequested { get; private set; }
        public TypedEventHandler<CoreWebView2, CoreWebView2ProcessFailedEventArgs>? WebView2_ProcessFailed { get; private set; }
        public TypedEventHandler<CoreWebView2, object>? WebView2_ContainsFullScreenElementChanged { get; private set; }

        // Update the MainWindow constructor to await the async method
        public MainWindow()
        {
            InitializeComponent();
            LoadVisitCounts();
            BrowserTabView.TabWidthMode = Microsoft.UI.Xaml.Controls.TabViewWidthMode.Equal;

            _appWindow = GetAppWindowForCurrentWindow();
            SetupTitleBar();
            InitializeBrowser();
            ApplyMicaToTitleBar();
            // ApplyWindowsExplorerTabBarStyle(); // Add this line

            CustomTitleBar.ActualThemeChanged += MainWindow_ActualThemeChanged;
            SetCloseButtonIcon();

            SearchBox.Loaded += (s, e) =>
            {
                var textBox = FindVisualChild<TextBox>(SearchBox);
                if (textBox != null)
                {
                    textBox.RightTapped -= TextBox_RightTapped;
                    textBox.RightTapped += TextBox_RightTapped;
                }
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"Unhandled exception: {e.ExceptionObject}");
            };

            Application.Current.UnhandledException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"UI thread unhandled exception: {e.Exception}");
                e.Handled = true;
            };


            try
            {
                this.SystemBackdrop = new MicaBackdrop
                {
                    Kind = MicaKind.BaseAlt
                };
            }
            catch
            {
                // Mica not supported, fallback or ignore
            }



            BrowserTabView.Loaded += (s, e) => AdjustTabWidths();
            BrowserTabView.SizeChanged += (s, e) => AdjustTabWidths();
        }

        private async Task<TabViewItem> CreateNewTab(string? url = null, string? title = "")
        {
            if (string.IsNullOrEmpty(title))
                title = $"New Tab {_tabCounter}";

            var tabItem = new TabViewItem
            {
                Header = title,
                IsClosable = true,
            };

            var webView = new Microsoft.UI.Xaml.Controls.WebView2();
            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.NavigationStarting += WebView_NavigationStarting;
            webView.CoreWebView2Initialized += WebView_CoreWebView2Initialized;
            MonitorWebViewEvents(webView);

            tabItem.Content = null;
            _tabWebViews[tabItem] = webView;

            BrowserTabView.TabItems.Add(tabItem);
            BrowserTabView.SelectedItem = tabItem;

            ContentGrid.Children.Clear();
            ContentGrid.Children.Add(webView);

            _tabCounter++;

            try
            {
                if (_isWindowClosed || webView == null || webView.CoreWebView2 != null)
                    return tabItem;

                if (!webView.IsLoaded)
                {
                    var tcs = new TaskCompletionSource();
                    RoutedEventHandler loadedHandler = null!;
                    loadedHandler = (s, e) =>
                    {
                        webView.Loaded -= loadedHandler;
                        tcs.SetResult();
                    };
                    webView.Loaded += loadedHandler;
                    await tcs.Task;
                }

                if (_isWindowClosed)
                    return tabItem;

                await webView.EnsureCoreWebView2Async();

                if (_isWindowClosed || webView.CoreWebView2 == null)
                    return tabItem;

                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "appassets", "Assets", CoreWebView2HostResourceAccessKind.Allow);
                webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(HammerJsMinified);
                await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(HammerJsGestureScript);

                // Inject smooth scroll CSS for programmatic scrolls
                await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(SmoothScrollCssScript);

                string pinchToZoomScript = @"(function() {
  if (window.__pinchZoomInjected) return;
  window.__pinchZoomInjected = true;
  let scale = 1;
  let minScale = 0.3;
  let maxScale = 3;
  let lastTouchDist = null;
  let ticking = false;
  document.body.style.transition = 'transform 0.18s cubic-bezier(.17,.67,.83,.67)';
  function applyScale(newScale) {
    scale = Math.max(minScale, Math.min(maxScale, newScale));
    document.body.style.transformOrigin = '0 0';
    document.body.style.transform = 'scale(' + scale + ')';
  }
  window.addEventListener('wheel', function(e) {
    if (e.ctrlKey) {
      e.preventDefault();
      let delta = -e.deltaY * 0.002;
      if (!ticking) {
        window.requestAnimationFrame(function() {
          applyScale(scale + delta);
          ticking = false;
        });
        ticking = true;
      }
    }
  }, { passive: false });
  window.addEventListener('touchmove', function(e) {
    if (e.touches.length === 2) {
      e.preventDefault();
      let dx = e.touches[0].clientX - e.touches[1].clientX;
      let dy = e.touches[0].clientY - e.touches[1].clientY;
      let dist = Math.sqrt(dx * dx + dy * dy);
      if (lastTouchDist) {
        let delta = (dist - lastTouchDist) * 0.005;
        if (!ticking) {
          window.requestAnimationFrame(function() {
            applyScale(scale + delta);
            ticking = false;
          });
          ticking = true;
        }
      }
      lastTouchDist = dist;
    }
  }, { passive: false });
  window.addEventListener('touchend', function(e) {
    if (e.touches.length < 2) {
      lastTouchDist = null;
    }
  });
  let lastTap = 0;
  window.addEventListener('touchend', function(e) {
    let now = Date.now();
    if (now - lastTap < 300) {
      scale = 1;
      document.body.style.transform = 'scale(1)';
    }
    lastTap = now;
  });
})();";
                string touchpadSwipeScript = @"(function() {
  if (window.__touchpadSwipeInjected) return;
  window.__touchpadSwipeInjected = true;
  
  let lastTime = 0;
  let lastDirection = null;
  let wheelEvents = [];
  let wheelTimer = null;
  
  function processWheelEvents() {
    if (wheelEvents.length === 0) return;
    
    // Calculate the total delta
    let totalDeltaX = 0;
    let totalDeltaY = 0;
    
    wheelEvents.forEach(e => {
      totalDeltaX += e.deltaX;
      totalDeltaY += e.deltaY;
    });
    
    // Clear the array
    wheelEvents = [];
    
    // Check if this is primarily a horizontal swipe
    if (Math.abs(totalDeltaX) > Math.abs(totalDeltaY) && Math.abs(totalDeltaX) > 40) {
      let now = Date.now();
      let direction = totalDeltaX > 0 ? 'right' : 'left';
      
      if (direction !== lastDirection || now - lastTime > 400) {
        console.log('Sending swipe: ' + direction + ' deltaX=' + totalDeltaX);
        
        window.chrome.webview && window.chrome.webview.postMessage(JSON.stringify({
          type: 'swipe',
          direction: direction,
          deltaX: totalDeltaX,
          deltaY: totalDeltaY,
          startX: 0,
          startY: 0
        }));
        
        lastTime = now;
        lastDirection = direction;
      }
    }
  }
  
  window.addEventListener('wheel', function(e) {
    // Collect wheel events and process them in batches
    wheelEvents.push(e);
    
    if (wheelTimer) {
      clearTimeout(wheelTimer);
    }
    
    wheelTimer = setTimeout(processWheelEvents, 50);
  }, { passive: true, capture: true }); // Use capture to get events before they're handled elsewhere
})();";
                const string PwaInstallScript = @"
window.addEventListener('beforeinstallprompt', function(e) {
    e.preventDefault();
    window.chrome.webview.postMessage(JSON.stringify({
        type: 'pwa-install-available'
    }));
    window.deferredPwaPrompt = e;
});
window.installPwa = function() {
    if (window.deferredPwaPrompt) {
        window.deferredPwaPrompt.prompt();
        window.deferredPwaPrompt = null;
    }
};
";
                const string EdgeLikeMomentumScrollScript = @"
(function() {
  if (window.__edgeMomentumScrollInjected) return;
  window.__edgeMomentumScrollInjected = true;

  // Only apply to mouse wheel, not touchpad/touch
  let scrollEl = document.scrollingElement || document.documentElement;
  let velocity = 0;
  let isMomentum = false;
  let momentumId = null;
  let lastWheelTime = 0;

  // Add smooth scroll CSS
  var style = document.createElement('style');
  style.textContent = `
    html, body, [style*='overflow'], [class*='scroll'], [class*='Scroll'] {
      scroll-behavior: smooth !important;
      -webkit-overflow-scrolling: touch !important;
    }
  `;
  document.head.appendChild(style);

  function momentumScroll() {
    if (Math.abs(velocity) < 0.1) {
      isMomentum = false;
      velocity = 0;
      return;
    }
    scrollEl.scrollTop += velocity;
    velocity *= 0.92; // friction
    momentumId = requestAnimationFrame(momentumScroll);
  }

  window.addEventListener('wheel', function(e) {
    // Only vertical scroll, not ctrl+wheel (pinch-zoom) or horizontal swipe
    if (e.ctrlKey || Math.abs(e.deltaX) > Math.abs(e.deltaY)) return;
    // Only apply to mouse wheel (not touchpad)
    if (e.deltaMode === 0 && Math.abs(e.deltaY) > 0) {
      velocity += e.deltaY * 0.7;
      if (!isMomentum) {
        isMomentum = true;
        momentumId = requestAnimationFrame(momentumScroll);
      }
      lastWheelTime = Date.now();
      e.preventDefault();
    }
  }, { passive: false });

  // Cancel momentum on user scroll (e.g., arrow keys, scrollbar drag)
  let lastScrollTop = scrollEl.scrollTop;
  window.addEventListener('scroll', function() {
    if (!isMomentum) return;
    if (Math.abs(scrollEl.scrollTop - lastScrollTop) < 1 && Date.now() - lastWheelTime > 100) {
      isMomentum = false;
      velocity = 0;
      if (momentumId) cancelAnimationFrame(momentumId);
    }
    lastScrollTop = scrollEl.scrollTop;
  }, { passive: true });

  // Re-apply smooth scroll if DOM changes (for SPAs)
  var observer = new MutationObserver(function() {
    if (!document.head.contains(style)) {
      document.head.appendChild(style);
    }
  });
  observer.observe(document.documentElement, { childList: true, subtree: true });
})();
";

                //await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                // "document.documentElement.style.scrollBehavior = 'smooth';"

                // await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(PwaInstallScript);
                await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(touchpadSwipeScript);
                // await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(jellyScrollScript);
                await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(pinchToZoomScript);
                //await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(SmoothMomentumScrollScript);
                await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(EdgeLikeMomentumScrollScript);
                // Removed the line that disposes the webView
                // Force all new window requests to open in the current tab
                webView.CoreWebView2.NewWindowRequested += (s, e) =>
                {
                    e.Handled = true;
                    webView.CoreWebView2.Navigate(e.Uri);
                };

                if (string.IsNullOrEmpty(url))
                {
                    var baseDir = AppContext.BaseDirectory;
                    var homepageFullPath = System.IO.Path.Combine(baseDir, HomepagePath);
                    var homepageUri = new Uri(homepageFullPath, UriKind.Absolute);
                    webView.Source = homepageUri;
                }
                else
                {
                    webView.CoreWebView2.Navigate(url);
                }
            }
            catch (Exception ex)
            {
                // Defensive: check if window is still open before showing dialog
                if (!_isWindowClosed)
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = $"Failed to initialize browser: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = Content.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }

            AttachWebView2FullscreenHandlers(webView);
            UpdateNavigationButtons();
            return tabItem;
        }

        private async void CloseTab(TabViewItem tabItem)
        {
            if (tabItem != null && _tabWebViews.ContainsKey(tabItem))
            {
                var webView = _tabWebViews[tabItem];

                if (ContentGrid.Children.Contains(webView))
                    ContentGrid.Children.Remove(webView);

                if (webView.CoreWebView2 != null)
                {
                    try
                    {
                        await webView.CoreWebView2.ExecuteScriptAsync(@"(async function() { /* ... */ })();");
                        webView.CoreWebView2.Navigate("about:blank");
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error cleaning up WebView2: {ex.Message}");
                    }
                }

                webView.NavigationCompleted -= WebView_NavigationCompleted;
                webView.NavigationStarting -= WebView_NavigationStarting;
                webView.CoreWebView2Initialized -= WebView_CoreWebView2Initialized;

                if (webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
                }

                _tabWebViews.Remove(tabItem);
                BrowserTabView.TabItems.Remove(tabItem);

                if (BrowserTabView.TabItems.Count > 0)
                {
                    var firstTab = BrowserTabView.TabItems[0] as TabViewItem;
                    if (firstTab != null && _tabWebViews.TryGetValue(firstTab, out var firstWebView))
                    {
                        ContentGrid.Children.Clear();
                        ContentGrid.Children.Add(firstWebView);
                        BrowserTabView.SelectedItem = firstTab;
                    }
                }
                else
                {
                    Close();
                }
            }
        }

        // Ensure the AddNewTabButton_Click method matches the RoutedEventHandler delegate signature.
        // The correct signature is: void AddNewTabButton_Click(object sender, RoutedEventArgs e)

        private void AddNewTabButton_Click(object sender, RoutedEventArgs e)
        {



            _ = CreateNewTab();



        }


        private Microsoft.UI.Xaml.Controls.WebView2? GetCurrentWebView()
        {
            if (BrowserTabView.SelectedItem is TabViewItem selectedTab &&
                _tabWebViews.TryGetValue(selectedTab, out var webView))
            {
                return webView;
            }
            return null;
        }

        private AppWindow? GetAppWindowForCurrentWindow()
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }

        private void SetupTitleBar()
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(CustomTitleBar);
        }

        private async void InitializeBrowser()
        {
            var firstTab = await CreateNewTab();
            if (_tabWebViews.TryGetValue(firstTab, out var webView))
            {
                ContentGrid.Children.Clear();
                ContentGrid.Children.Add(webView);
                UpdateNavigationButtons();
            }
        }

        private void TextBox_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is TextBox textBox)
            {
                var menu = new MenuFlyout();

                var copyItem = new MenuFlyoutItem { Text = "Copy link" };
                copyItem.Click += (s, args) =>
                {
                    var dataPackage = new DataPackage();
                    var toCopy = !string.IsNullOrEmpty(textBox.SelectedText) ? textBox.SelectedText : textBox.Text;
                    dataPackage.SetText(toCopy);
                    Clipboard.SetContent(dataPackage);
                };

                menu.Items.Add(copyItem);
                menu.ShowAt(textBox, e.GetPosition(textBox));
            }
        }

        private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
                return;

            var query = sender.Text?.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                sender.ItemsSource = null;
                return;
            }

            try
            {
                using var http = new HttpClient();
                var url = $"https://suggestqueries.google.com/complete/search?client=firefox&q={Uri.EscapeDataString(query)}";
                var response = await http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);
                var suggestions = doc.RootElement[1].EnumerateArray()
                    .Select(x => x.GetString())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();
                sender.ItemsSource = suggestions;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching suggestions: {ex.Message}");
                sender.ItemsSource = null;
            }
        }

        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is string suggestion)
            {
                sender.Text = suggestion;
            }
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var text = args.QueryText?.Trim();
            if (string.IsNullOrEmpty(text))
                return;

            bool isUrl = Uri.TryCreate(text, UriKind.Absolute, out _) ||
                         (text.Contains(".") && !text.Contains(" "));

            string navigateUrl = isUrl
                ? (text.StartsWith("http") ? text : $"https://{text}")
                : $"https://www.google.com/search?q={Uri.EscapeDataString(text)}";

            var webView = GetCurrentWebView();
            if (webView?.CoreWebView2 != null)
            {
                webView.CoreWebView2.Navigate(navigateUrl);
            }
        }

        private async void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var messageString = e.TryGetWebMessageAsString();

                // Handle favicon messages
                if (messageString.Contains("\"type\":\"favicon\""))
                {
                    var json = JsonDocument.Parse(messageString);
                    var faviconUrl = json.RootElement.GetProperty("url").GetString();
                    if (!string.IsNullOrEmpty(faviconUrl))
                    {
                        var webView = sender as Microsoft.UI.Xaml.Controls.WebView2;
                        var tabItem = _tabWebViews.FirstOrDefault(x => x.Value == webView).Key;
                        if (tabItem != null)
                        {
                            // Resolve relative URLs
                            if (!Uri.TryCreate(faviconUrl, UriKind.Absolute, out var iconUri) && webView?.CoreWebView2 != null)
                            {
                                var baseUri = new Uri(webView.CoreWebView2.Source);
                                iconUri = new Uri(baseUri, faviconUrl);
                            }
                            tabItem.IconSource = new BitmapIconSource
                            {
                                UriSource = iconUri,
                                ShowAsMonochrome = false
                            };
                        }
                    }
                    return;
                }

                // Handle swipe messages - make sure this is properly parsed
                if (messageString.Contains("\"type\":\"swipe\""))
                {
                    var message = JsonSerializer.Deserialize<SwipeMessage>(messageString);
                    if (message?.type == "swipe" && !string.IsNullOrEmpty(message.direction))
                    {
                        System.Diagnostics.Debug.WriteLine($"Swipe detected: {message.direction} (deltaX={message.deltaX})");
                        await HandleSwipeGesture(message.direction, message.deltaX, message.deltaY);
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing web message: {ex.Message}");
            }
        }

        private async Task HandleSwipeGesture(string direction, double deltaX, double deltaY)
        {
            // Log detailed info for debugging
            System.Diagnostics.Debug.WriteLine($"Processing swipe: {direction}, deltaX={deltaX}, deltaY={deltaY}");

            // Visual feedback
            if (direction == "left")
                ShowRightSwipeFeedback();
            else if (direction == "right")
                ShowLeftSwipeFeedback();

            DispatcherQueue.TryEnqueue(() =>
            {
                var currentWebView = GetCurrentWebView();

                switch (direction)
                {
                    case "left":
                        if (IsEdgeSwipe(deltaX, deltaY, "left"))
                        {
                            if (currentWebView?.CoreWebView2?.CanGoBack == true)
                            {
                                System.Diagnostics.Debug.WriteLine("Edge swipe left detected - going back");
                                currentWebView.CoreWebView2.GoBack();
                            }
                        }
                        break;
                    case "right":
                        if (IsEdgeSwipe(deltaX, deltaY, "right"))
                        {
                            if (currentWebView?.CoreWebView2?.CanGoForward == true)
                            {
                                // Replace this line:
                                // if (firstTab != null && _tabWebViews.tryGetValue(firstTab, out var firstWebView))

                                // With this:
                                //if (firstTab != null && _tabWebViews.TryGetValue(firstTab, out var firstWebView))
                                System.Diagnostics.Debug.WriteLine("Edge swipe right detected - going forward");
                                currentWebView.CoreWebView2.GoForward();
                            }
                        }
                        break;
                }
            });
            await Task.CompletedTask;
        }

        private bool IsEdgeSwipe(double deltaX, double deltaY, string direction)
        {
            const double edgeThreshold = 50.0;
            const double minSwipeDistance = 100.0;

            var windowBounds = AppWindow.Size;

            bool isLongSwipe = Math.Abs(deltaX) > minSwipeDistance;
            bool isPrimaryDirection = Math.Abs(deltaX) > Math.Abs(deltaY) * 2; // Mostly horizontal

            return isLongSwipe && isPrimaryDirection;
        }

        private async Task HandlePinchGesture(double scale)
        {
            var currentWebView = GetCurrentWebView();
            if (currentWebView?.CoreWebView2 != null)
            {
                System.Diagnostics.Debug.WriteLine($"Pinch gesture detected: scale = {scale}");
            }
            await Task.CompletedTask;
        }

        private async void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (sender is Microsoft.UI.Xaml.Controls.WebView2 webView)
            {
                var tabItem = _tabWebViews.FirstOrDefault(x => x.Value == webView).Key;
                if (tabItem != null && webView.CoreWebView2 != null)
                {
                    // Remove progress bar with fade-out animation
                    if (_tabProgressBars.TryGetValue(tabItem, out var progressBar))
                    {
                        progressBar.IsIndeterminate = false;
                        progressBar.Value = 100;

                        // Fade out animation
                        var fadeOut = new DoubleAnimation
                        {
                            From = 1.0,
                            To = 0.0,
                            Duration = new Duration(TimeSpan.FromMilliseconds(300))
                        };

                        Storyboard.SetTarget(fadeOut, progressBar);
                        Storyboard.SetTargetProperty(fadeOut, "Opacity");

                        Storyboard storyboard = new Storyboard();
                        storyboard.Children.Add(fadeOut);
                        storyboard.Completed += (s, e) =>
                        {
                            Grid tabContainer = tabItem.FindDescendant<Grid>();
                            if (tabContainer != null)
                            {
                                tabContainer.Children.Remove(progressBar);
                            }
                            _tabProgressBars.Remove(tabItem);
                        };

                        storyboard.Begin();
                    }

                    // Always set the tab header to the page title
                    string title = webView.CoreWebView2.DocumentTitle ?? "Untitled";
                    if (title.Length > 25)
                        title = title.Substring(0, 25) + "...";
                    tabItem.Header = title;

                    // Inject favicon extraction script after navigation
                    await webView.CoreWebView2.ExecuteScriptAsync(@"
(function() {
    var links = document.getElementsByTagName('link');
    var icon = '';
    for (var i = 0; i < links.length; i++) {
        if ((links[i].rel || '').toLowerCase().includes('icon') && links[i].href) {
            icon = links[i].href;
            break;
        }
    }
    if (icon) {
        window.chrome.webview.postMessage(JSON.stringify({ type: 'favicon', url: icon }));
    }
})();
");

                    // Fallback: If no favicon was set by JS, try /favicon.ico
                    if (tabItem.IconSource == null || tabItem.IconSource is SymbolIconSource)
                    {
                        tabItem.IconSource = await GetFaviconIconSourceAsync(webView.CoreWebView2.Source);
                    }

                    // Set desktop wallpaper as homepage background
                    if (webView.CoreWebView2.Source.Replace('/', '\\').EndsWith(HomepagePath, StringComparison.OrdinalIgnoreCase))
                    {
                        var wallpaperPath = GetDesktopWallpaperPath();
                        if (!string.IsNullOrEmpty(wallpaperPath) && File.Exists(wallpaperPath))
                        {
                            var wallpaperUri = new Uri(wallpaperPath).AbsoluteUri.Replace("file:///", "file:///");
                            string js = $@"
                                (function() {{
                                    document.body.style.backgroundImage = 'url(""{wallpaperUri}"")';
                                    document.body.style.backgroundSize = 'cover';
                                    document.body.style.backgroundPosition = 'center';
                                    document.body.style.backgroundRepeat = 'no-repeat';
                                }})();
                            ";
                            await webView.CoreWebView2.ExecuteScriptAsync(js);
                        }
                    }
                }

                if (webView.CoreWebView2 != null && SearchBox != null)
                {
                    var url = webView.CoreWebView2.Source;
                    if (!url.Replace('/', '\\').EndsWith(HomepagePath, StringComparison.OrdinalIgnoreCase))
                    {
                        SearchBox.Text = url;
                    }
                    else
                    {
                        SearchBox.Text = string.Empty;
                    }
                }

                UpdateNavigationButtons();

                if (webView.CoreWebView2 != null)
                {
                    TrackVisit(webView.CoreWebView2.Source);
                }

                // If homepage is currently displayed, update most visited section
                if (webView.CoreWebView2 != null &&
                    webView.CoreWebView2.Source.Replace('/', '\\').EndsWith(HomepagePath, StringComparison.OrdinalIgnoreCase))
                {
                    ApplyMicaToTitleBar();
                    await InjectMostVisitedAsync(webView);
                }

                System.Diagnostics.Debug.WriteLine($"Navigation completed for: {webView.CoreWebView2?.Source}");
            }
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            _isPwaInstallAvailable = false;

            if (sender is Microsoft.UI.Xaml.Controls.WebView2 webView)
            {
                var tabItem = _tabWebViews.FirstOrDefault(x => x.Value == webView).Key;
                if (tabItem != null)
                {
                    tabItem.Header = "Loading...";

                    // Add progress bar to the tab
                    ProgressBar progressBar = new ProgressBar
                    {
                        IsIndeterminate = true,
                        Height = 2,
                        Foreground = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };

                    // Store reference to the progress bar
                    _tabProgressBars[tabItem] = progressBar;

                    // Add progress bar to tab
                    Grid tabContainer = tabItem.FindDescendant<Grid>();
                    if (tabContainer != null)
                    {
                        tabContainer.Children.Add(progressBar);
                    }
                }
            }
        }

        private void WebView_CoreWebView2Initialized(object sender, CoreWebView2InitializedEventArgs e)
        {
            if (sender is Microsoft.UI.Xaml.Controls.WebView2 webView && webView.CoreWebView2 != null)
            {
                // Enable developer tools, password autosave, etc.
                webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = true;
                webView.CoreWebView2.Settings.AreHostObjectsAllowed = true;
            }
        }

        private void UpdateNavigationButtons()
        {
            if (GetCurrentWebView() is Microsoft.UI.Xaml.Controls.WebView2 webView && webView.CoreWebView2 != null)
            {
                BackButton.IsEnabled = webView.CoreWebView2.CanGoBack;
                // Update the back button state whenever navigation state changes
                System.Diagnostics.Debug.WriteLine($"Back button enabled: {webView.CoreWebView2.CanGoBack}, Forward available: {webView.CoreWebView2.CanGoForward}");
            }
            else
            {
                BackButton.IsEnabled = false;
            }
        }

      

        private void MainWindow_ActualThemeChanged(FrameworkElement sender, object args)
{
    SetCloseButtonIcon();
    CustomTitleBar.RequestedTheme = this.ActualTheme;

    // Update Windows Explorer styles
   // ApplyWindowsExplorerTabBarStyle();
    
    // Refresh acrylic effect on the selected tab when theme changes
    if (BrowserTabView.SelectedItem is TabViewItem selectedTab)
    {
        ApplyAcrylicToSelectedTab(selectedTab);
    }
}

        private void SetCloseButtonIcon()
        {
            CloseIconLight = CustomTitleBar.FindName("CloseIconLight") as FrameworkElement;
            CloseIconDark = CustomTitleBar.FindName("CloseIconDark") as FrameworkElement;
            if (CloseIconLight == null || CloseIconDark == null)
                return;

            var theme = CustomTitleBar.ActualTheme;

            if (theme == ElementTheme.Dark)
            {
                CloseIconLight.Visibility = Visibility.Collapsed;
                CloseIconDark.Visibility = Visibility.Visible;
            }
            else
            {
                CloseIconLight.Visibility = Visibility.Visible;
                CloseIconDark.Visibility = Visibility.Collapsed;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var webView = GetCurrentWebView();
            if (webView == null)
            {
                System.Diagnostics.Debug.WriteLine("No WebView2 instance found for the selected tab.");
                return;
            }

            if (webView.CoreWebView2 == null)
            {
                System.Diagnostics.Debug.WriteLine("CoreWebView2 is not initialized.");
                return;
            }

            if (webView.CoreWebView2.CanGoBack)
            {
                webView.CoreWebView2.GoBack();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No page to go back to.");
            }
        }

        private void GoButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            // TODO: Implement navigation logic here
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is AutoSuggestBox box && !string.IsNullOrEmpty(box.Text))
            {
                var textBox = FindVisualChild<TextBox>(box);
                textBox?.SelectAll();
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                    return tChild;
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void SearchBox_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            e.Handled = true;

            if (sender is AutoSuggestBox box)
            {
                var textBox = FindVisualChild<TextBox>(box);

                var menu = new MenuFlyout();

                var copyItem = new MenuFlyoutItem { Text = "Copy link" };
                copyItem.Click += (s, args) =>
                {
                    var dataPackage = new DataPackage();
                    var toCopy = textBox != null && !string.IsNullOrEmpty(textBox.SelectedText)
                        ? textBox.SelectedText
                        : box.Text;
                    dataPackage.SetText(toCopy);
                    Clipboard.SetContent(dataPackage);
                };

                menu.Items.Add(copyItem);
                menu.ShowAt(box, e.GetPosition(box));
            }
        }

        private void BrowserTabView_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            // Only track tab manipulations, not WebView ones
            var originalSource = e.OriginalSource as DependencyObject;

            // Debug the source to identify potential issues
            System.Diagnostics.Debug.WriteLine($"Manipulation started on: {originalSource?.GetType().Name}");

            if (IsDescendantOfWebView2(originalSource))
            {
                _tabSwipeStartX = double.NaN;
                e.Complete(); // Let the WebView handle its own manipulations
                return;
            }

            _tabSwipeStartX = e.Position.X;
        }

        private void BrowserTabView_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (double.IsNaN(_tabSwipeStartX))
                return;

            double deltaX = e.Position.X - _tabSwipeStartX;
            const double swipeThreshold = 60;

            if (Math.Abs(deltaX) > swipeThreshold)
            {
                if (deltaX < 0)
                {
                    // Swipe left: go to next tab
                }
                else
                {
                    // Swipe right: go to previous tab
                }
            }
        }

        private bool IsDescendantOfWebView2(DependencyObject? obj)
        {
            while (obj != null)
            {
                if (obj is Microsoft.UI.Xaml.Controls.WebView2)
                    return true;
                obj = VisualTreeHelper.GetParent(obj);
            }
            return false;
        }

        private async Task<IconSource> GetFaviconIconSourceAsync(string url)
        {
            try
            {
                var uri = new Uri(url);
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                {
                    var faviconUrl = $"{uri.Scheme}://{uri.Host}/favicon.ico";
                    using var http = new HttpClient();
                    var response = await http.GetAsync(faviconUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var ms = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
                        var bitmap = new BitmapImage();
                        ms.Position = 0;
                        await bitmap.SetSourceAsync(ms.AsRandomAccessStream());
                        return new BitmapIconSource
                        {
                            UriSource = new Uri(faviconUrl),
                            ShowAsMonochrome = false
                        };
                    }
                }
            }
            catch { }
            return new SymbolIconSource { Symbol = Symbol.Globe };
        }

        // Helper class for deserializing web messages
        private class SwipeMessage
        {
            public string? type { get; set; }
            public string? direction { get; set; }
            public double deltaX { get; set; }
            public double deltaY { get; set; }
            public double scale { get; set; }
            public double startX { get; set; }
            public double startY { get; set; }


            // Example: Load an ONNX model and run inference with OpenVINO (Intel) backend


            public void Dispose()
            {
                //  _session?.Dispose();
                //_session = null;
            }
        }

        private void AttachWebView2FullscreenHandlers(Microsoft.UI.Xaml.Controls.WebView2 webView)
        {
            if (webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.ContainsFullScreenElementChanged += (sender, args) =>
                {
                    if (_isWindowClosed || _appWindow == null)
                        return;

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            if (_isWindowClosed || _appWindow == null)
                                return;

                            var presenter = _appWindow.Presenter;
                            if (presenter == null || !_appWindow.IsVisible)
                                return;

                            if (webView.CoreWebView2.ContainsFullScreenElement)
                            {
                                if (presenter.Kind != AppWindowPresenterKind.FullScreen &&
                                    presenter is OverlappedPresenter)
                                {
                                    _appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                                }

                                // Hide the custom title bar and tab bar
                                if (CustomTitleBar != null)
                                    CustomTitleBar.Visibility = Visibility.Collapsed;
                                if (TabBarGrid != null)
                                    TabBarGrid.Visibility = Visibility.Collapsed;

                                ExtendsContentIntoTitleBar = false;
                                SetTitleBar(null);
                            }
                            else
                            {
                                if (presenter.Kind != AppWindowPresenterKind.Overlapped &&
                                    presenter is OverlappedPresenter)
                                {
                                    _appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
                                }

                                // Show the custom title bar and tab bar
                                if (CustomTitleBar != null)
                                    CustomTitleBar.Visibility = Visibility.Visible;
                                if (TabBarGrid != null)
                                    TabBarGrid.Visibility = Visibility.Visible;

                                ExtendsContentIntoTitleBar = true;
                                SetTitleBar(CustomTitleBar);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to set presenter: {ex.GetType()}: {ex.Message}\n{ex.StackTrace}");
                        }
                    });
                };
            }
            else
            {
                webView.CoreWebView2Initialized += (s, e) =>
                {
                    AttachWebView2FullscreenHandlers(webView);
                };
            }
        }



        private void ApplyMicaToTitleBar()
        {
            // Mica effect removed. No action needed.
        }

        // 1. Track visits (already present in your code)
        //private readonly Dictionary<string, int> _visitCounts = new;

        // 2. Inject most visited sites into the homepage
        private async Task InjectMostVisitedAsync(Microsoft.UI.Xaml.Controls.WebView2 webView)
        {
            var topSites = GetMostVisited(10);
            var jsArray = string.Join(",", topSites.Select(url => $"'{url}'"));
            var js = $@"
        if (window.setMostVisitedLinks) {{
            window.setMostVisitedLinks([{jsArray}]);
        }}
    ";
            await webView.ExecuteScriptAsync(js);
        }



        private SettingsWindow? _settingsWindow = null;

        // Replace all occurrences of _settingsWindow.Show(); with _settingsWindow.Activate();
        // Example fix in SettingsButton_Click:
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if settings window is already open
            if (_settingsWindow != null)
            {
                // If already open, just activate it
                _settingsWindow.Activate();
                return;
            }

            // Create and show new settings window
            _settingsWindow = new SettingsWindow();
            _settingsWindow.Activate();

            // When the window is closed, clear the reference
            _settingsWindow.Closed += (s, args) =>
            {
                _settingsWindow = null;
            };
        }



        private async void ShowLeftSwipeFeedback()
        {
            LeftSwipeFeedbackOverlay.Visibility = Visibility.Visible;
            LeftSwipeFeedbackOverlay.Opacity = 0;
            var sb = (Storyboard)LeftSwipeFeedbackOverlay.Resources["LeftSwipeFadeStoryboard"];
            sb.Begin();
            await Task.Delay(520);
            LeftSwipeFeedbackOverlay.Visibility = Visibility.Collapsed;
        }

        private async void ShowRightSwipeFeedback()
        {
            RightSwipeFeedbackOverlay.Visibility = Visibility.Visible;
            RightSwipeFeedbackOverlay.Opacity = 0;
            var sb = (Storyboard)RightSwipeFeedbackOverlay.Resources["RightSwipeFadeStoryboard"];
            sb.Stop();
            sb.Begin();
            await Task.Delay(520);
            RightSwipeFeedbackOverlay.Visibility = Visibility.Collapsed;
        }

        private void StartSearchLoadingAnimation()
        {
            SearchLoadingLine.Visibility = Visibility.Visible;
            SearchLoadingLine.Width = 2; // Reset

            var startStoryboard = (Storyboard)RootGrid.Resources["StartLoadingLineStoryboard"];
            var animation = (DoubleAnimation)startStoryboard.Children[0];
            animation.From = 0;
            animation.To = SearchContainer.ActualWidth;

            startStoryboard.Begin();
        }

        private void FinishSearchLoadingAnimation()
        {
            var finishStoryboard = (Storyboard)RootGrid.Resources["FinishLoadingLineStoryboard"];
            var animation = (DoubleAnimation)finishStoryboard.Children[0];
            animation.From = SearchLoadingLine.Width;
            animation.To = SearchContainer.ActualWidth;

            finishStoryboard.Completed += (s, e) =>
            {
                SearchLoadingLine.Visibility = Visibility.Collapsed;
                SearchLoadingLine.Width = 2;
            };
            finishStoryboard.Begin();
        }
        // Add this method to your MainWindow class to fix CS1061
        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            var webView = GetCurrentWebView();
            if (webView != null && webView.CanGoForward)
            {
                webView.GoForward();
            }
        }
        // Add this method to your MainWindow class to fix CS1061
        private void SiteInfoButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            // TODO: Implement site info logic here, e.g., show site information dialog or flyout
        }
        // Add this method to your MainWindow class to fix CS1061
        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement share functionality here.
        }

        // url: the webpage URL (e.g., "https://example.com")
        // tabViewItem: the TabViewItem to update
        async Task SetFaviconAsync(string pageUrl, TabViewItem tab)
        {
            try
            {
                var uri = new Uri(pageUrl);
                var faviconUrl = $"{uri.Scheme}://{uri.Host}/favicon.ico";

                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(faviconUrl);
                if (!response.IsSuccessStatusCode)
                    return; // Fallback or set a default icon

                var stream = await response.Content.ReadAsStreamAsync();
                var memStream = new InMemoryRandomAccessStream();
                var outputStream = memStream.GetOutputStreamAt(0);
                await RandomAccessStream.CopyAsync(stream.AsInputStream(), outputStream);
                await outputStream.FlushAsync();

                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(memStream);

                tab.IconSource = new BitmapIconSource
                {
                    UriSource = new Uri(faviconUrl)
                };
            }
            catch
            {
                // Optionally set a default icon or handle errors
            }
        }

        private readonly Dictionary<string, int> _visitCounts = new();
        private static readonly string VisitCountsFile =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App3", "visitcounts.json");
        // Add this field to your MainWindow class to fix CS0103
        private readonly Dictionary<TabViewItem, ProgressBar> _tabProgressBars = new();
        // Call this method after every successful navigation
        private void TrackVisit(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
                return;

            if (_visitCounts.ContainsKey(url))
                _visitCounts[url]++;
            else
                _visitCounts[url] = 1;

            SaveVisitCounts();

            // Update homepage if it's currently displayed
            var webView = GetCurrentWebView();
            if (webView?.CoreWebView2 != null &&
                webView.CoreWebView2.Source.Replace('/', '\\').EndsWith(HomepagePath, StringComparison.OrdinalIgnoreCase))
            {
                _ = InjectMostVisitedAsync(webView);
            }
        }

        private void SaveVisitCounts()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(VisitCountsFile)!);
                File.WriteAllText(VisitCountsFile, JsonSerializer.Serialize(_visitCounts));
            }
            catch { /* Handle or log errors as needed */ }
        }

        private void LoadVisitCounts()
        {
            try
            {
                if (File.Exists(VisitCountsFile))
                {
                    var json = File.ReadAllText(VisitCountsFile);
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                    if (loaded != null)
                    {
                        _visitCounts.Clear();
                        foreach (var kv in loaded)
                            _visitCounts[kv.Key] = kv.Value;
                    }
                }
            }
            catch { /* Handle or log errors as needed */ }
        }

        private List<string> GetMostVisited(int count = 10)
        {
            var validSites = _visitCounts
                .OrderByDescending(kv => kv.Value)
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && !kv.Key.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Key)
                .ToList();

            // Optionally skip the first entry if you know it's always broken
            if (validSites.Count > 0 && IsBrokenUrl(validSites[0]))
                validSites.RemoveAt(0);

            return validSites.Take(count).ToList();
        }

        // Helper to check for a broken URL (customize as needed)
        private bool IsBrokenUrl(string url)
        {
            // Example: skip if not http/https or matches a known broken pattern
            return !url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                || url.Contains("broken") // Add more conditions as needed
                || url == "about:blank";
        }

        // Call this method to toggle immersive (full screen) mode
        private void ToggleImmersiveMode()
        {
            if (_appWindow == null || CustomTitleBar == null)
                return;

            if (!_isImmersive)
            {
                _appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                CustomTitleBar.Visibility = Visibility.Collapsed;
                _isImmersive = true;
            }
            else
            {
                _appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
                CustomTitleBar.Visibility = Visibility.Visible;
                _isImmersive = false;
            }
        }

        private async void ShowTabOverview()
        {
            var tabPreviewModels = new List<TabOverviewPage.TabPreviewModel>();

            foreach (var kv in _tabWebViews)
            {
                var tab = kv.Key;
                var webView = kv.Value;
                var title = tab.Header?.ToString() ?? "Tab";

                // Capture preview
                var rtb = new RenderTargetBitmap();
                await rtb.RenderAsync(webView);
                var pixelBuffer = await rtb.GetPixelsAsync();
                var bitmap = new BitmapImage();
                using (var stream = new InMemoryRandomAccessStream())
                {
                    var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(
                        Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream);
                    encoder.SetPixelData(
                        Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
                        Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,
                        (uint)rtb.PixelWidth,
                        (uint)rtb.PixelHeight,
                        96, 96,
                        pixelBuffer.ToArray());
                    await encoder.FlushAsync();
                    stream.Seek(0);
                    await bitmap.SetSourceAsync(stream);
                }

                tabPreviewModels.Add(new TabOverviewPage.TabPreviewModel
                {
                    Title = title,
                    PreviewImage = bitmap,
                    TabReference = tab
                });
            }

            var overviewPage = new TabOverviewPage();
            overviewPage.SetTabs(tabPreviewModels);
            overviewPage.TabCloseRequested += (tab) => CloseTab(tab);

            var dialog = new ContentDialog
            {
                Content = overviewPage,
                XamlRoot = this.Content.XamlRoot,
                PrimaryButtonText = "Done",
                IsPrimaryButtonEnabled = true
            };
            await dialog.ShowAsync();
        }

        private void ShowTabs_Click(object sender, RoutedEventArgs e)
        {
            ShowTabOverview();
        }

        public class TabItemViewModel
        {
            public string Title { get; set; }
            public BitmapImage PreviewImage { get; set; }
        }
        // Move VisualTreeExtensions to the top level (outside of MainWindow) to fix CS1109

        private void UpdateTabPreviews()
        {
            foreach (var tab in BrowserTabView.TabItems)
            {
                var tabViewItem = tab as TabViewItem;
                if (tabViewItem != null)
                {
                    var content = tabViewItem.Content as FrameworkElement;
                    if (content != null)
                    {
                        var renderTargetBitmap = new RenderTargetBitmap();
                        // RenderAsync is async, but for preview purposes, you may want to await it or use .GetAwaiter().GetResult() in sync context
                        var renderTask = renderTargetBitmap.RenderAsync(content);
                        renderTask.AsTask().Wait();

                        // Fix CS0029: Await the BitmapToImageSource task to get BitmapImage
                        var bitmapImageTask = BitmapToImageSource(renderTargetBitmap);
                        bitmapImageTask.Wait();
                        var bitmapImage = bitmapImageTask.Result;

                        var tabItemViewModel = new TabItemViewModel
                        {
                            Title = tabViewItem.Header.ToString(),
                            PreviewImage = bitmapImage
                        };

                        tabViewItem.DataContext = tabItemViewModel;
                    }
                }
            }
        }

        private async Task<BitmapImage> BitmapToImageSource(RenderTargetBitmap renderTargetBitmap)
        {
            var bitmapImage = new BitmapImage();
            using (var stream = new InMemoryRandomAccessStream())
            {
                await renderTargetBitmap.GetPixelsAsync().AsTask().ContinueWith(async pixels =>
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied,
                        (uint)renderTargetBitmap.PixelWidth,
                        (uint)renderTargetBitmap.PixelHeight,
                        96, 96,
                        pixels.Result.ToArray());
                    await encoder.FlushAsync();
                });

                bitmapImage.SetSource(stream);
            }
            return bitmapImage;
        }

        private static string? GetDesktopWallpaperPath()
        {
            const int SPI_GETDESKWALLPAPER = 0x0073;
            const int MAX_PATH = 260;
            var sb = new System.Text.StringBuilder(MAX_PATH);
            if (SystemParametersInfo(SPI_GETDESKWALLPAPER, MAX_PATH, sb, 0))
                return sb.ToString();
            return null;
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SystemParametersInfo(int uAction, int uParam, System.Text.StringBuilder lpvParam, int fuWinIni);
        // Add this helper method to find parent of a specific type
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            
            if (parent == null)
                return null;
                
            if (parent is T typedParent)
                return typedParent;
                
            return FindParent<T>(parent);
        }

        // Add this method to directly style the tab using TabView templates
        private void ApplyDirectTabStyling(TabViewItem tab, bool isSelected)
        {
            // Find the core tab template parts using deeper visual tree search
            var tabPresenter = tab.FindDescendant<ContentPresenter>();
            if (tabPresenter != null)
            {
                // Apply styles directly to the content presenter
                if (isSelected)
                {
                    // Selected state
                    tabPresenter.Background = new SolidColorBrush(
                        ActualTheme == ElementTheme.Dark
                            ? Windows.UI.Color.FromArgb(190, 45, 45, 55)
                            : Windows.UI.Color.FromArgb(190, 235, 240, 250)
                    );
                    
                    // Try to add a border if needed
                    var parentGrid = FindParent<Grid>(tabPresenter);
                    if (parentGrid != null)
                    {
                        var border = new Border
                        {
                            Background = new SolidColorBrush(Colors.Transparent),
                            BorderThickness = new Thickness(0, 0, 0, 2),
                            BorderBrush = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]),
                            Child = tabPresenter
                        };
                        
                        // Replace presenter with bordered version
                        int index = parentGrid.Children.IndexOf(tabPresenter);
                        if (index >= 0)
                        {
                            parentGrid.Children.RemoveAt(index);
                            parentGrid.Children.Insert(index, border);
                        }
                    }
                }
                else // Non-selected state
                {
                    tabPresenter.Background = new SolidColorBrush(Colors.Transparent);
                }
            }
        }

        // Add this method to your MainWindow class to fix CS1061
        private void BrowserTabView_TabCloseRequested(Microsoft.UI.Xaml.Controls.TabView sender, Microsoft.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs args)
        {
            // Example implementation: close the requested tab
            if (args.Tab is Microsoft.UI.Xaml.Controls.TabViewItem tabItem)
            {
                CloseTab(tabItem);
            }
        }

        private async void WebView_CoreWebView2Initialized(Microsoft.UI.Xaml.Controls.WebView2 sender, CoreWebView2InitializedEventArgs e)
        {
            if (sender.CoreWebView2 != null)
            {
                // Enable developer tools, password autosave, etc.
                sender.CoreWebView2.Settings.AreDevToolsEnabled = true;
                sender.CoreWebView2.Settings.IsStatusBarEnabled = true;
                sender.CoreWebView2.Settings.AreHostObjectsAllowed = true;
            }

            // Favicon handling
            sender.CoreWebView2.WebMessageReceived += (s, e) =>
            {
                var messageString = e.TryGetWebMessageAsString();
                if (messageString.Contains("\"type\":\"favicon\""))
                {
                    var json = JsonDocument.Parse(messageString);
                    var faviconUrl = json.RootElement.GetProperty("url").GetString();
                    if (!string.IsNullOrEmpty(faviconUrl))
                    {
                        var tabItem = _tabWebViews.FirstOrDefault(x => x.Value == sender).Key;
                        if (tabItem != null)
                        {
                            tabItem.IconSource = new BitmapIconSource
                            {
                                UriSource = new Uri(faviconUrl, UriKind.RelativeOrAbsolute),
                                ShowAsMonochrome = false
                            };
                        }
                    }
                }
            };

            // Execute script to find and return the favicon URL
            await sender.CoreWebView2.ExecuteScriptAsync(@"
(function() {
    var links = document.getElementsByTagName('link');
    var icon = '';
    for (var i = 0; i < links.length; i++) {
        if ((links[i].rel || '').toLowerCase().includes('icon') && links[i].href) {
            icon = links[i].href;
            break;
        }
    }
    if (icon) {
        window.chrome.webview.postMessage(JSON.stringify({ type: 'favicon', url: icon }));
    }
})();
");
        }

        private void MonitorWebViewEvents(Microsoft.UI.Xaml.Controls.WebView2 webView)
        {
            webView.PointerPressed += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"WebView pointer pressed: {e.GetCurrentPoint((UIElement)s).Properties.PointerUpdateKind}");
            };

            webView.PointerMoved += (s, e) =>
            {
                if (e.Pointer.IsInContact)
                    System.Diagnostics.Debug.WriteLine($"WebView pointer moved while in contact");
            };

            webView.ManipulationStarted += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"WebView manipulation started");
            };
        }

        private void ApplyAcrylicToSelectedTab(TabViewItem selectedTab)
        {
            if (selectedTab == null) return;
            
            System.Diagnostics.Debug.WriteLine("Applying acrylic effect to tab: " + selectedTab.Header);
            
            // Process all tabs
            foreach (TabViewItem tabItem in BrowserTabView.TabItems)
            {
                bool isSelected = tabItem == selectedTab;
                
                try {
                    // Find the root grid of the tab item (deeper visual tree inspection)
                    var tabHeaderGrid = tabItem.FindDescendant<Grid>(g => 
                        g.Name == "HeaderGrid" || 
                        g.Name == "TabViewItemInner" ||
                        g.Name == "TabViewItemGrid" ||
                        (g.Children.Count > 0 && g.Children.OfType<ContentPresenter>().Any()));
                        
                    if (tabHeaderGrid == null)
                    {
                        // Fallback: get any Grid in the tab
                        tabHeaderGrid = tabItem.FindDescendant<Grid>();
                    }
                    
                    if (tabHeaderGrid != null)
                    {
                        // Style for selected tab
                        if (isSelected)
                        {
                            // Direct styling approach using a semi-transparent solid color brush for reliability
                            tabHeaderGrid.Background = new SolidColorBrush(
                                ActualTheme == ElementTheme.Dark
                                    ?Windows.UI.Color.FromArgb(190, 45, 45, 55)
                                    : Windows.UI.Color.FromArgb(190, 235, 240, 250)
                            );
                            
                            // Apply elevation shadow
                            tabItem.Translation = new System.Numerics.Vector3(0, 0, 6);
                            
                            // Create and add an accent color indicator at the bottom
                            var indicator = new Border
                            {
                                Height = 2,
                                VerticalAlignment = VerticalAlignment.Bottom,
                                HorizontalAlignment = HorizontalAlignment.Stretch,
                                Margin = new Thickness(8, 0, 8, 0),
                                CornerRadius = new CornerRadius(1),
                                Background = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"])
                            };
                            
                            // Remove any existing indicators first
                            var existingIndicators = tabHeaderGrid.Children
                                .OfType<Border>()
                                .Where(b => b.Height <= 3)
                                .ToList();
                                
                            foreach (var existing in existingIndicators)
                            {
                                tabHeaderGrid.Children.Remove(existing);
                            }
                            
                            // Add the new indicator
                            tabHeaderGrid.Children.Add(indicator);
                            
                            // Try applying true acrylic effect as a separate attempt (might work on some systems)
                            try {
                                var acrylicBrush = GetTabAcrylicBrush();
                                tabHeaderGrid.Background = acrylicBrush;
                            }
                            catch (Exception ex) {
                                System.Diagnostics.Debug.WriteLine($"Acrylic fallback used: {ex.Message}");
                                // Already applied the solid color fallback
                            }
                        }
                        else // Non-selected tab styling
                        {
                            // Reset background to transparent for non-selected tabs
                            tabHeaderGrid.Background = new SolidColorBrush(Colors.Transparent);
                            
                            // Reset elevation
                            tabItem.Translation = new System.Numerics.Vector3(0, 0, 0);
                            
                            // Remove any indicators
                            var indicators = tabHeaderGrid.Children
                                .OfType<Border>()
                                .Where(b => b.Height <= 3)
                                .ToList();
                                
                            foreach (var indicator in indicators)
                            {
                                tabHeaderGrid.Children.Remove(indicator);
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Failed to find tab header container");
                    }
                }
                catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"Error styling tab: {ex.Message}");
                }
            }
        }

        // Improved acrylic brush creation specifically for tabs
        private Brush GetTabAcrylicBrush()
        {
            try
            {
                return new Microsoft.UI.Xaml.Media.AcrylicBrush
                {
                    TintColor = ActualTheme == ElementTheme.Dark 
                        ? Windows.UI.Color.FromArgb(200, 40, 40, 50)  // More opaque for dark theme
                        : Windows.UI.Color.FromArgb(200, 230, 235, 245), // More opaque for light theme
                    TintOpacity = ActualTheme == ElementTheme.Dark ? 0.85 : 0.6,
                    TintLuminosityOpacity = 0.9,
                    FallbackColor = ActualTheme == ElementTheme.Dark
                        ? Windows.UI.Color.FromArgb(225, 35, 35, 45)
                        : Windows.UI.Color.FromArgb(225, 225, 230, 240)
                };
            }
            catch
            {
                // Fallback to solid color if acrylic fails
                return new SolidColorBrush(
                    ActualTheme == ElementTheme.Dark 
                        ? Windows.UI.Color.FromArgb(200, 40, 40, 50)
                        : Windows.UI.Color.FromArgb(200, 230, 235, 245)
                );
            }
        }

        private void AdjustTabWidths()
        {
            if (BrowserTabView.TabItems.Count == 0)
                return;

            // Calculate available width
            double availableWidth = BrowserTabView.ActualWidth;
            if (availableWidth <= 0)
                return;
            
            // Account for margins, padding, add button, etc.
            availableWidth -= 60; // Adjust this value based on your layout
            
            // Calculate width per tab (with a minimum)
            double tabWidth = Math.Max(150, availableWidth / BrowserTabView.TabItems.Count);
            
            // Apply width to each tab
            foreach (var tabItem in BrowserTabView.TabItems.OfType<TabViewItem>())
            {
                tabItem.MinWidth = tabWidth;
                tabItem.MaxWidth = tabWidth;
            }
        }
        

    }
}