using System.Windows;
using System.Windows.Controls;

namespace ShowMeTheXAML
{
    public class XamlDisplay : ContentControl
    {
        static XamlDisplay()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(XamlDisplay), new FrameworkPropertyMetadata(typeof(XamlDisplay)));
        }

        public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(
            nameof(Key), typeof(string), typeof(XamlDisplay), new PropertyMetadata(default(string), OnKeyChanged));

        private static void OnKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is XamlDisplay xamlDisplay)
            {
                xamlDisplay.SetCurrentValue(XamlProperty, XamlResolver.Resolve((string)e.NewValue));
            }
        }

        public string Key
        {
            get => (string) GetValue(KeyProperty);
            set => SetValue(KeyProperty, value);
        }

        public static readonly DependencyProperty XamlProperty = DependencyProperty.Register(
            nameof(Xaml), typeof(string), typeof(XamlDisplay), new PropertyMetadata(default(string)));

        public string Xaml
        {
            get => (string) GetValue(XamlProperty);
            set => SetValue(XamlProperty, value);
        }
    }
}
