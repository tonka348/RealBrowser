using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace App3.Controls
{
    public sealed partial class BrowserHomePage : UserControl
    {
        private readonly List<string> _searchSuggestions = new List<string>();
        private readonly List<string> _recentSearches = new List<string>();
        private const int MaxRecentSearches = 10;
        private const int MaxRecentTabs = 10;

        public event EventHandler<string>? SearchRequested;

        public BrowserHomePage()
        {
            InitializeComponent();
            PopulateDefaultSuggestions();
        }

        private void PopulateDefaultSuggestions()
        {
            // Common search suggestions
            _searchSuggestions.AddRange(new[] {
                "weather",
                "news today",
                "maps",
                "translate",
                "email",
                "calendar",
                "videos",
                "images",
                "shopping",
                "restaurants near me"
            });
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string query = args.QueryText.Trim();
            if (!string.IsNullOrEmpty(query))
            {
                // Add to recent searches
                AddToRecentSearches(query);
                
                // Notify parent about search request
                SearchRequested?.Invoke(this, query);
            }
        }

        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is string selectedSuggestion)
            {
                sender.Text = selectedSuggestion;
            }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                string query = sender.Text.ToLower().Trim();
                
                if (string.IsNullOrEmpty(query))
                {
                    // Show recent searches when box is empty
                    sender.ItemsSource = _recentSearches;
                }
                else
                {
                    // Filter suggestions based on input
                    var suggestions = new List<string>();
                    
                    // First add matching recent searches
                    suggestions.AddRange(_recentSearches
                        .Where(s => s.ToLower().Contains(query))
                        .Take(3));
                        
                    // Then add other matching suggestions
                    suggestions.AddRange(_searchSuggestions
                        .Where(s => s.ToLower().Contains(query) && !suggestions.Contains(s))
                        .Take(5));
                        
                    // Add the exact query as a suggestion if it's not already in the list
                    if (!suggestions.Contains(query) && query.Length > 1)
                    {
                        suggestions.Add($"Search for \"{query}\"");
                    }
                    
                    sender.ItemsSource = suggestions;
                }
            }
        }
        
        private void AddToRecentSearches(string query)
        {
            // Remove if exists (to avoid duplicates)
            _recentSearches.Remove(query);
            
            // Add to beginning of list
            _recentSearches.Insert(0, query);
            
            // Trim list if needed
            while (_recentSearches.Count > MaxRecentSearches)
            {
                _recentSearches.RemoveAt(_recentSearches.Count - 1);
            }
        }
        
        private void QuickLinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string url)
            {
                SearchRequested?.Invoke(this, url);
            }
        }
        
        public void FocusSearchBox()
        {
            SearchBox?.Focus(FocusState.Programmatic);
        }
    }
}