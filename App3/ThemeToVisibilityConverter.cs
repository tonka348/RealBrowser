using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace App3
{
    public class ThemeToVisibilityConverter : IValueConverter
    {
        public string TargetTheme { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Normalize TargetTheme for comparison
            var targetTheme = TargetTheme?.Trim().ToLowerInvariant();

            if (value is ElementTheme theme)
            {
                // Compare enum as string, case-insensitive
                return theme.ToString().ToLowerInvariant() == targetTheme
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            if (value is string themeStr)
            {
                // Compare string, case-insensitive
                return themeStr.Trim().ToLowerInvariant() == targetTheme
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}