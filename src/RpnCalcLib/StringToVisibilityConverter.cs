namespace Menees.RpnCalc
{
	#region Using Directives

	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;

	#endregion

	public class StringToVisibilityConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string text = value as string;
			Visibility result = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
			return result;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			// For RPN calc's use cases, it never makes sense to convert a Visibility back into a string.
			// We only need one-way binding support from this converter.
			// https://stackoverflow.com/a/265544/1882616
			return DependencyProperty.UnsetValue;
		}

		#endregion
	}
}
