#region Using Directives

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

#endregion

namespace Menees.RpnCalc
{
	// This technique came from the second answer at:
	// http://stackoverflow.com/questions/397556/wpf-how-to-bind-radiobuttons-to-an-enum
	// Usage example:
	// <StackPanel>
	//    <StackPanel.Resources>
	//        <rpn:EnumToBoolValueConverter x:Key="EnumToBoolConverter" />
	//    </StackPanel.Resources>
	//    <RadioButton IsChecked="{Binding Path=YourEnumProperty, Converter={StaticResource EnumToBoolConverter}, ConverterParameter=Enum1}" />
	//    <RadioButton IsChecked="{Binding Path=YourEnumProperty, Converter={StaticResource EnumToBoolConverter}, ConverterParameter=Enum2}" />
	// </StackPanel>
	public class EnumToBoolConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			object parameterValue = GetParameterValue(value.GetType(), parameter);
			bool result = value.Equals(parameterValue);
			return result;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value.Equals(false))
			{
				return DependencyProperty.UnsetValue;
			}
			else
			{
				object parameterValue = GetParameterValue(targetType, parameter);
				return parameterValue;
			}
		}

		#endregion

		#region Private Methods

		private static object GetParameterValue(Type enumType, object parameter)
		{
			// Silverlight doesn't support the x:Static tag, so we can't pass a converter parameter
			// like {x:Static rpn:AngleMode.Degrees}.  Instead, we have to pass the string "Degrees"
			// and parse that into the appropriate enum value at run-time.
			object parameterValue = Enum.Parse(enumType, parameter.ToString(), false);
			return parameterValue;
		}

		#endregion
	}
}
