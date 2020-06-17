using System;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

#if __UNO__
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows;
using System.Windows.Controls;
#endif

namespace ShowMeTheXAML
{
    public partial class XamlDisplay : ContentControl
    {
#if __UNO__
        public static XName XmlName => XName.Get(nameof(XamlDisplay), $"using:{nameof(ShowMeTheXAML)}");
#else
        private static readonly string AssemblyName = typeof(XamlDisplay).Assembly.GetName().Name;
        public static XName XmlName => XName.Get(nameof(XamlDisplay), $"clr-namespace:{nameof(ShowMeTheXAML)};assembly={AssemblyName}");
#endif

        static XamlDisplay()
        {
#if !__UNO__
            DefaultStyleKeyProperty.OverrideMetadata(typeof(XamlDisplay), new FrameworkPropertyMetadata(typeof(XamlDisplay)));
#endif
        }

        internal const string IgnorePropertyName = "Ignore";
        public static readonly DependencyProperty IgnoreProperty = DependencyProperty.RegisterAttached(
            IgnorePropertyName, typeof(Scope), typeof(XamlDisplay), new PropertyMetadata(default(Scope)));

        public static void SetIgnore(DependencyObject element, Scope value)
            => element.SetValue(IgnoreProperty, value);

        public static Scope GetIgnore(DependencyObject element)
            => (Scope)element.GetValue(IgnoreProperty);

        public static void Init(params Assembly[] assemblies)
        {
            if (assemblies?.Any() == true)
            {
                foreach (Assembly assembly in assemblies)
                {
                    LoadFromAssembly(assembly);
                }
            }

#if __ANDROID__ || __WASM__
            LoadFromAssembly(Assembly.GetCallingAssembly());
#else
            LoadFromAssembly(Assembly.GetEntryAssembly());
#endif

            void LoadFromAssembly(Assembly assembly)
            {
                Type xamlDictionary = assembly?.GetType("ShowMeTheXAML.XamlDictionary");
                if (xamlDictionary != null)
                {
                    //Invoke the static constructor
                    xamlDictionary.TypeInitializer.Invoke(null, null);
                }
            }
        }

        #region Property: UniqueKey
        public static readonly DependencyProperty UniqueKeyProperty = DependencyProperty.Register(
            nameof(UniqueKey),
            typeof(string),
            typeof(XamlDisplay),
            new PropertyMetadata(default(string), OnUniqueKeyChanged));

        public string UniqueKey
        {
            get => (string)GetValue(UniqueKeyProperty);
            set => SetValue(UniqueKeyProperty, value);
        }

        private static void OnUniqueKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is XamlDisplay xamlDisplay)
            {
                xamlDisplay.ReloadXaml();
            }
        }
        #endregion

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
        private string _key;

        private void ReloadXaml()
        {
            if (_isLoading) return;
            string key = UniqueKey;
            string xaml = XamlResolver.Resolve(key);
            IXamlFormatter formatter = Formatter ?? XamlFormatter.Default;
            if (formatter != null)
            {
                xaml = formatter.FormatXaml(xaml);
            }
            _isLoading = true;
#if __UNO__
            Xaml = xaml;
#else
            SetCurrentValue(XamlProperty, xaml);
#endif
            _isLoading = false;
        }
    }
}
