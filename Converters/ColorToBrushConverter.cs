using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace DeadEye.Converters;

internal sealed class ColorToBrushConverter : MarkupExtension, IValueConverter
{
	private static ColorToBrushConverter? _converter;

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is Color color)
			return new SolidColorBrush(color);

		return new SolidColorBrush(Colors.Magenta);
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return _converter ??= new ColorToBrushConverter();
	}
}
