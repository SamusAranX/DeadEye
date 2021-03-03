using System;
using System.Windows.Media;

namespace DeadEye.Extensions {
	/// <summary>
	/// Defines a color in Hue/Saturation/Value (HSV) space.
	/// </summary>
	public struct HsvColor {
		/// <summary>
		/// The Hue in 0..360 range.
		/// </summary>
		public double H;

		/// <summary>
		/// The Saturation in 0..1 range.
		/// </summary>
		public double S;

		/// <summary>
		/// The Value in 0..1 range.
		/// </summary>
		public double V;

		/// <summary>
		/// The Alpha/opacity in 0..1 range.
		/// </summary>
		public double A;
	}

	public static class ColorExtensions {
		/// <summary>
		/// Converts a <see cref="Color"/> to a hexadecimal string representation.
		/// </summary>
		/// <param name="color">The color to convert.</param>
		/// <returns>The hexadecimal string representation of the color.</returns>
		public static string ToHex(this Color color) {
			return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
		}

		/// <summary>
		/// Converts a <see cref="Color"/> to a premultiplied Int32 - 4 byte ARGB structure.
		/// </summary>
		/// <param name="color">The color to convert.</param>
		/// <returns>The int representation of the color.</returns>
		public static int ToInt(this Color color) {
			var a = color.A + 1;
			var col = (color.A << 24) | ((byte)((color.R * a) >> 8) << 16) | ((byte)((color.G * a) >> 8) << 8) | (byte)((color.B * a) >> 8);
			return col;
		}

		/// <summary>
		/// Converts a <see cref="Color"/> to an <see cref="HsvColor"/>.
		/// </summary>
		/// <param name="color">The <see cref="Color"/> to convert.</param>
		/// <returns>The converted <see cref="HsvColor"/>.</returns>
		public static HsvColor ToHsv(this Color color) {
			const double toDouble = 1.0 / 255;
			var r = toDouble * color.R;
			var g = toDouble * color.G;
			var b = toDouble * color.B;
			var max = Math.Max(Math.Max(r, g), b);
			var min = Math.Min(Math.Min(r, g), b);
			var chroma = max - min;
			double h1;

			if (chroma == 0) {
				h1 = 0;
			} else if (Math.Abs(max - r) < double.Epsilon) {
				// The % operator doesn't do proper modulo on negative
				// numbers, so we'll add 6 before using it
				h1 = (((g - b) / chroma) + 6) % 6;
			} else if (Math.Abs(max - g) < double.Epsilon) {
				h1 = 2 + ((b - r) / chroma);
			} else {
				h1 = 4 + ((r - g) / chroma);
			}

			var saturation = chroma == 0 ? 0 : chroma / max;
			HsvColor ret;
			ret.H = 60 * h1;
			ret.S = saturation;
			ret.V = max;
			ret.A = toDouble * color.A;
			return ret;
		}

		private static double sRGBToLinear(double component) {
			component /= byte.MaxValue;

			if (component <= 0.03928) {
				component /= 12.92;
			} else {
				component = Math.Pow((component + 0.055) / 1.055, 2.4);
			}

			return component;
		}

		public static double GetLuma(this Color c) {
			var r = sRGBToLinear(c.R);
			var g = sRGBToLinear(c.G);
			var b = sRGBToLinear(c.B);

			return 0.2126 * r + 0.7152 * g + 0.0722 * b;
		}
	}
}
