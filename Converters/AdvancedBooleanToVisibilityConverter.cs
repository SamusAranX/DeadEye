using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace DeadEye.Converters;

internal sealed class AdvancedBooleanToVisibilityConverter : MarkupExtension, IValueConverter
{
	private static AdvancedBooleanToVisibilityConverter? _converter;

	private readonly BooleanToVisibilityConverter _boolToVisConverter = new();

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var input = false;
		if (value is bool valueBool)
			input = valueBool;

		if (parameter is bool and true)
			input = !input;

		return this._boolToVisConverter.Convert(input, targetType, null, null);
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return this._boolToVisConverter.ConvertBack(value, targetType, parameter, culture);
	}

	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return _converter ??= new AdvancedBooleanToVisibilityConverter();
	}
}
