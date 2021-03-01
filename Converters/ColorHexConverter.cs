using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace DeadEye.Converters {
	internal class ColorHexConverter: MarkupExtension, IValueConverter {
		private static ColorHexConverter _converter;
		public override object ProvideValue(IServiceProvider serviceProvider) {
			return _converter ??= new ColorHexConverter();
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if (value == null)
				return "??????";

			var c = (Color)value;
			return $"{c.R:X2}{c.G:X2}{c.B:X2}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}