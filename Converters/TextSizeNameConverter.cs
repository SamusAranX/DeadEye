using System.Globalization;
using System.Windows.Data;

namespace DeadEye.Converters;

internal sealed class TextSizeNameConverter : IValueConverter
{
	private readonly Dictionary<double, string> _textSizeNames = new()
	{
		{ 10, "XSmall" },
		{ 11, "Small" },
		{ 12, "Medium" },
		{ 13, "Large" },
		{ 14, "XLarge" },
		{ 15, "XXLarge" },
		{ 16, "Largest" },
	};

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null)
			return "N/A";

		if (!this._textSizeNames.ContainsKey((double)value))
			return ((double)value).ToString(CultureInfo.InvariantCulture);

		return this._textSizeNames[(double)value];
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
