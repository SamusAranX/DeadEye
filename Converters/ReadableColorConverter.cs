using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using DeadEye.Extensions;

namespace DeadEye.Converters;

internal sealed class ReadableColorConverter : MarkupExtension, IValueConverter
{
	private static ReadableColorConverter? _converter;

	private static readonly SolidColorBrush BLACK = Brushes.Black;
	private static readonly SolidColorBrush WHITE = Brushes.White;

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		Color color;

		if (value is SolidColorBrush brush)
			color = brush.Color;
		else if (value is Color c)
			color = c;
		else
		{
			Debug.WriteLine("no color!");
			return BLACK;
		}
			

		var lightness = color.GetLightness();
		return lightness < 0.5 ? WHITE : BLACK;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return _converter ??= new ReadableColorConverter();
	}
}
