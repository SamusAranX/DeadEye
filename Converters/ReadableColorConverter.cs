using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using DeadEye.Extensions;

namespace DeadEye.Converters {
	internal class ReadableColorConverter: MarkupExtension, IValueConverter {
		private static ReadableColorConverter _converter;
		public override object ProvideValue(IServiceProvider serviceProvider) {
			return _converter ??= new ReadableColorConverter();
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if (value == null)
				value = Colors.White;

			if (value is SolidColorBrush brush)
				value = brush.Color;

			var luma = ((Color)value).GetLuma();
			return luma < 0.4 ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}