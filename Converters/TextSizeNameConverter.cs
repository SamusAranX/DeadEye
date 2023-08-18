using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace DeadEye.Converters;

internal sealed class TextSizeNameConverter : MarkupExtension, IValueConverter
{
	private static TextSizeNameConverter? _converter;

	private readonly Dictionary<double, string> _textSizeNames = new()
	{
		{ 11, "Smaller" },
		{ 12, "Small" },
		{ 13, "Medium" },
		{ 14, "Large" },
		{ 15, "Larger" },
	};

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not double textSize)
			return "N/A";

		if (this._textSizeNames.TryGetValue(textSize, out var textSizeName))
			return textSizeName;
		
		return textSize.ToString(CultureInfo.InvariantCulture);
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return _converter ??= new TextSizeNameConverter();
	}
}
