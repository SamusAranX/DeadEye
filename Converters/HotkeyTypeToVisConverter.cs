using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using DeadEye.Hotkeys;

namespace DeadEye.Converters;

internal sealed class HotkeyTypeToVisConverter : MarkupExtension, IValueConverter
{
	private static HotkeyTypeToVisConverter? _converter;

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
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

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return _converter ??= new HotkeyTypeToVisConverter();
	}
}
