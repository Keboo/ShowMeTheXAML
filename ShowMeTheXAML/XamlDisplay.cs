using System;
using System.Linq;
using System.Reflection;
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

        public static void Init(params Assembly[] assemblies)
        {
            if (assemblies?.Any() == true)
            {
                foreach (Assembly assembly in assemblies)
                {
                    LoadFromAssembly(assembly);
                }
            }
            LoadFromAssembly(Assembly.GetEntryAssembly());

            void LoadFromAssembly(Assembly assembly)
            {
                Type xamlDictionary = assembly.GetType("ShowMeTheXAML.XamlDictionary");
                if (xamlDictionary != null)
                {
                    //Invoke the static constructor
                    xamlDictionary.TypeInitializer.Invoke(null, null);
                }
            }
        }

        public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(
            nameof(Key), typeof(string), typeof(XamlDisplay), new PropertyMetadata(default(string), OnKeyChanged));

        private static void OnKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is XamlDisplay xamlDisplay)
            {
                xamlDisplay.ReloadXaml();
            }
        }

        public string Key
        {
            get => (string) GetValue(KeyProperty);
            set => SetValue(KeyProperty, value);
        }

        public static readonly DependencyProperty XamlProperty = DependencyProperty.Register(
            nameof(Xaml), typeof(string), typeof(XamlDisplay), new PropertyMetadata(default(string), OnXamlChanged));

        private static void OnXamlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is XamlDisplay xamlDisplay)
            {
                xamlDisplay.ReloadXaml();
            }
        }

        public string Xaml
        {
            get => (string) GetValue(XamlProperty);
            set => SetValue(XamlProperty, value);
        }

        public static readonly DependencyProperty FormatterProperty = DependencyProperty.Register(
            nameof(Formatter), typeof(IXamlFormatter), typeof(XamlDisplay), 
            new PropertyMetadata(default(IXamlFormatter), OnXamlConverterChanged));

        private static void OnXamlConverterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is XamlDisplay xamlDisplay)
            {
                xamlDisplay.ReloadXaml();
            }
        }

        public IXamlFormatter Formatter
        {
            get => (IXamlFormatter) GetValue(FormatterProperty);
            set => SetValue(FormatterProperty, value);
        }

        private bool _isLoading;
        private void ReloadXaml()
        {
            if (_isLoading) return;
            string key = Key;
            string xaml = XamlResolver.Resolve(key);
            IXamlFormatter formatter = Formatter;
            if (formatter != null)
            {
                xaml = formatter.FormatXaml(xaml);
            }
            _isLoading = true;
            SetCurrentValue(XamlProperty, xaml);
            _isLoading = false;
        }
    }
}
