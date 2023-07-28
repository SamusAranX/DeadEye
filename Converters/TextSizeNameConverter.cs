using System.Globalization;
using System.Windows.Data;

namespace DeadEye.Converters;

internal sealed class TextSizeNameConverter : IValueConverter
{
	private readonly Dictionary<double, string> _textSizeNames = new()
	{
		{ 11, "Smaller" },
		{ 12, "Small" },
		{ 13, "Medium" },
		{ 14, "Large" },
		{ 15, "Larger" },
	};

	public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not double textSize)
			return "N/A";

		if (this._textSizeNames.TryGetValue(textSize, out var textSizeName))
			return textSizeName;
		
		return textSize.ToString(CultureInfo.InvariantCulture);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
