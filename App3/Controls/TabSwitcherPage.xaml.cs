using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace App3.Controls
{
    public sealed partial class TabSwitcherPage : UserControl
    {
        public TabSwitcherPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Populate the tab switcher with tab previews.
        /// </summary>
        /// <param name="tabs">A list of (Title, Url, BitmapImage? PreviewImage) for each tab.</param>
        /// <param name="selectedIndex">The index of the currently selected tab.</param>
        public void SetTabs(IEnumerable<(string Title, string Url, BitmapImage? PreviewImage)> tabs, int selectedIndex)
        {
            TabsPanel.Items.Clear();
            int i = 0;
            foreach (var tab in tabs)
            {
                var border = new Border
                {
                    Width = 200,
                    Height = 160,
                    Margin = new Thickness(4),
                    CornerRadius = new CornerRadius(12),
                    BorderThickness = new Thickness(i == selectedIndex ? 4 : 1),
                    BorderBrush = i == selectedIndex
                        ? new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue)
                        : new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                    Background = new SolidColorBrush(Microsoft.UI.Colors.White)
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                var title = new TextBlock
                {
                    Text = tab.Title,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = 16,
                    Margin = new Thickness(10, 6, 10, 0),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetRow(title, 0);
                grid.Children.Add(title);

                var image = new Image
                {
                    Source = tab.PreviewImage,
                    Stretch = Stretch.UniformToFill,
                    Margin = new Thickness(10, 2, 10, 10)
                };
                Grid.SetRow(image, 1);
                grid.Children.Add(image);

                border.Child = grid;

                int tabIndex = i;
                border.Tapped += (s, e) =>
                {
                    TabSelected?.Invoke(this, tabIndex);
                };

                TabsPanel.Items.Add(border);
                i++;
            }
        }

        /// <summary>
        /// Raised when a tab preview is selected.
        /// </summary>
        public event TabSelectedEventHandler? TabSelected;
        public delegate void TabSelectedEventHandler(object sender, int tabIndex);
    }
}