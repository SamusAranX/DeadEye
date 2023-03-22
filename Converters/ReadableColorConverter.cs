using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using DeadEye.Extensions;

namespace DeadEye.Converters;

internal sealed class ReadableColorConverter : MarkupExtension, IValueConverter
{
	private static ReadableColorConverter _converter;

	private static readonly SolidColorBrush MAGENTA = new(Colors.Magenta);
	private static readonly SolidColorBrush BLACK = new(Colors.Black);
	private static readonly SolidColorBrush WHITE = new(Colors.White);

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null)
			return BLACK;

		if (value is SolidColorBrush brush)
			value = brush.Color;

		var color = (Color)value;
		if (color.A <= 102)
			return BLACK;

		var luma = color.GetLuma();
		return luma < 0.4 ? WHITE : BLACK;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return _converter ??= new ReadableColorConverter();
	}
}
