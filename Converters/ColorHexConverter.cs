using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace DeadEye.Converters;

internal sealed class ColorHexConverter : MarkupExtension, IValueConverter
{
	private static ColorHexConverter? _converter;

	public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is null or not Color)
			return "N/A";

		var c = (Color)value;
		if (parameter is true)
			return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

		return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return _converter ??= new ColorHexConverter();
	}
}
