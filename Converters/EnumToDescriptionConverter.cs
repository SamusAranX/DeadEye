using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace DeadEye.Converters;

internal sealed class EnumToDescriptionConverter : MarkupExtension, IValueConverter
{
	private static EnumToDescriptionConverter? _converter;

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value == null)
			return string.Empty;

		var type = value.GetType();

		return type.IsEnum ? GetDescription(type, value) : string.Empty;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	private static string GetDescription(Type enumType, object enumValue)
	{
		var enumValueStr = enumValue.ToString()!;

		return
			enumType
				.GetField(enumValueStr)!
				.GetCustomAttributes(typeof(DescriptionAttribute), false)
				.FirstOrDefault() is DescriptionAttribute descriptionAttribute
				? descriptionAttribute.Description
				: enumValueStr;
	}

	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return _converter ??= new EnumToDescriptionConverter();
	}
}
