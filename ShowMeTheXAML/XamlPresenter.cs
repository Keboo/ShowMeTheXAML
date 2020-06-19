using System;
using System.Collections.Generic;
using System.Text;

#if __UNO__
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using System.Windows;
using System.Windows.Controls;
#endif

namespace ShowMeTheXAML
{
	public partial class XamlPresenter : Control
	{
		#region Property: ReferenceKey

		public static DependencyProperty ReferenceKeyProperty { get; } = DependencyProperty.Register(
			nameof(ReferenceKey),
			typeof(string),
			typeof(XamlPresenter),
			new PropertyMetadata(default, OnReferenceKeyChanged));

		public string ReferenceKey
		{
			get => (string)GetValue(ReferenceKeyProperty);
			set => SetValue(ReferenceKeyProperty, value);
		}

		#endregion
		#region Property: Formatter

		public static DependencyProperty FormatterProperty { get; } = DependencyProperty.Register(
			nameof(Formatter),
			typeof(IXamlFormatter),
			typeof(XamlPresenter),
			new PropertyMetadata(default, OnFormatterChanged));

		public IXamlFormatter Formatter
		{
			get => (IXamlFormatter)GetValue(FormatterProperty);
			set => SetValue(FormatterProperty, value);
		}

		#endregion
		#region Property: Xaml

		public static DependencyProperty XamlProperty { get; } = DependencyProperty.Register(
			nameof(Xaml),
			typeof(string),
			typeof(XamlPresenter),
			new PropertyMetadata(default));

		public string Xaml
		{
			get => (string)GetValue(XamlProperty);
			set => SetValue(XamlProperty, value);
		}

		#endregion

		private static void OnReferenceKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as XamlPresenter)?.ReloadXaml();

		private static void OnFormatterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as XamlPresenter)?.ReloadXaml();

		private void ReloadXaml()
		{
			string key = ReferenceKey;
			string xaml = XamlResolver.Resolve(key);

			IXamlFormatter formatter = Formatter;
			if (formatter != null)
			{
				xaml = formatter.FormatXaml(xaml);
			}

#if __UNO__
			Xaml = xaml;
#else
			SetCurrentValue(XamlProperty, xaml);
#endif
		}
	}
}
