using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace App3
{
    public class ThemeProxy : FrameworkElement
    {
        public ElementTheme CurrentTheme
        {
            get { return (ElementTheme)GetValue(CurrentThemeProperty); }
            set { SetValue(CurrentThemeProperty, value); }
        }

        public static readonly DependencyProperty CurrentThemeProperty =
            DependencyProperty.Register(nameof(CurrentTheme), typeof(ElementTheme), typeof(ThemeProxy), new PropertyMetadata(ElementTheme.Default));

        public void Attach(FrameworkElement element)
        {
            UpdateTheme(element.ActualTheme);
            element.ActualThemeChanged += (s, e) => UpdateTheme(element.ActualTheme);
        }

        private void UpdateTheme(ElementTheme theme)
        {
            CurrentTheme = theme;
        }
    }
}