using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace DeadEye.Converters;

internal sealed class PickerSizeDoubleConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
	{
		// convert PickerSize to double
		if (value is not PickerSize pickerSize)
			return -1;

		return (double)pickerSize;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		// convert double back to PickerSize
		if (value is not double pickerSize)
			throw new ArgumentException("pickerSize is not a double");

		var pickerSizeInt = (int)pickerSize;
		if (!Enum.IsDefined(typeof(PickerSize), pickerSizeInt))
			throw new InvalidEnumArgumentException("invalid value for pickerSize");

		return (PickerSize)pickerSizeInt;
	}
}
