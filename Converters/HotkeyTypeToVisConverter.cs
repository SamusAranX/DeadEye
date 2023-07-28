using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using DeadEye.Hotkeys;

namespace DeadEye.Converters;

internal sealed class HotkeyTypeToVisConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
	{
		Debug.WriteLine("hotkey type: {0}", value);
		if (value == null)
			return Visibility.Collapsed;

		if (value is HotkeyType hotkeyValue)
		{
			if (parameter is not HotkeyType hotkeyParameter)
				return Visibility.Visible;

			return hotkeyValue == hotkeyParameter;
		}
		
		throw new ArgumentException("Invalid HotkeyType value");
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
