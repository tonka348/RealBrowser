using System;
using System.Globalization;
using System.Windows.Data;

namespace App3
{
    public class WebView2SourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Pass-through or implement your conversion logic here
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Pass-through or implement your conversion logic here
            return value;
        }
    }
}
