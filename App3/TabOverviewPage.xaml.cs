using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace App3
{
    public sealed partial class TabOverviewPage : Page
    {
        public class TabPreviewModel
        {
            public string Title { get; set; }
            public BitmapImage PreviewImage { get; set; }
            public TabViewItem TabReference { get; set; }
        }

        public delegate void TabCloseRequestedHandler(TabViewItem tab);
        public event TabCloseRequestedHandler? TabCloseRequested;

        public TabOverviewPage()
        {
            this.InitializeComponent();
        }

        public void SetTabs(IEnumerable<TabPreviewModel> tabs)
        {
            TabItemsControl.ItemsSource = tabs;
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TabViewItem tab)
            {
                TabCloseRequested?.Invoke(tab);
            }
        }
    }
}