using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace DeadEye.Converters {
	public class EnumToDescriptionConverter: IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if (value == null) {
				return string.Empty;
			}

			var type = value.GetType();

			return type.IsEnum ? this.GetDescription(type, value) : string.Empty;
		}

		private string GetDescription(Type enumType, object enumValue) {
			return
				enumType
					.GetField(enumValue.ToString())
					.GetCustomAttributes(typeof(DescriptionAttribute), false)
					.FirstOrDefault() is DescriptionAttribute descriptionAttribute
					? descriptionAttribute.Description
					: enumValue.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}