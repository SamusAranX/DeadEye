using System.Windows.Media;

namespace DeadEye.Extensions;

/// <summary>
/// Defines a color in Hue/Saturation/Value (HSV) space.
/// </summary>
public sealed class HsvColor
{
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

public static class ColorExtensions
{
	/// <summary>
	/// The CIE standard states 0.008856 but 216/24389 is the intent for 0.008856451679036
	/// </summary>
	private const double EPSILON = 216d / 24389d;

	/// <summary>
	/// The CIE standard states 903.3, but 24389/27 is the intent, making 903.296296296296296
	/// </summary>
	private const double KAPPA = 24389d / 27d;

	private const double THIRD = 1d / 3d;

	private const double SRGB_COEFF_R = 0.2126;
	private const double SRGB_COEFF_G = 0.7152;
	private const double SRGB_COEFF_B = 0.0722;

	/// <summary>
	/// Converts a <see cref="Color" /> to a hexadecimal string representation.
	/// </summary>
	/// <param name="color">The color to convert.</param>
	/// <returns>The hexadecimal string representation of the color.</returns>
	public static string ToHex(this Color color)
	{
		return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
	}

	/// <summary>
	/// Converts a <see cref="Color" /> to a premultiplied Int32 - 4 byte ARGB structure.
	/// </summary>
	/// <param name="color">The color to convert.</param>
	/// <returns>The int representation of the color.</returns>
	public static int ToInt(this Color color)
	{
		var a = color.A + 1;
		var col = (color.A << 24) | ((byte)((color.R * a) >> 8) << 16) | ((byte)((color.G * a) >> 8) << 8) | (byte)((color.B * a) >> 8);
		return col;
	}

	/// <summary>
	/// Converts a <see cref="Color" /> to an <see cref="HsvColor" />.
	/// </summary>
	/// <param name="color">The <see cref="Color" /> to convert.</param>
	/// <returns>The converted <see cref="HsvColor" />.</returns>
	public static HsvColor ToHsv(this Color color)
	{
		const double TO_DOUBLE = 1.0 / 255;
		var r = TO_DOUBLE * color.R;
		var g = TO_DOUBLE * color.G;
		var b = TO_DOUBLE * color.B;
		var max = Math.Max(Math.Max(r, g), b);
		var min = Math.Min(Math.Min(r, g), b);
		var chroma = max - min;
		double h1;

		if (chroma == 0)
			h1 = 0;
		else if (Math.Abs(max - r) < double.Epsilon)
			/*
			 * The % operator doesn't do proper modulo on negative
			 * numbers, so we'll add 6 before using it
			 */
			h1 = ((g - b) / chroma + 6) % 6;
		else if (Math.Abs(max - g) < double.Epsilon)
			h1 = 2 + (b - r) / chroma;
		else
			h1 = 4 + (r - g) / chroma;

		var saturation = chroma == 0 ? 0 : chroma / max;
		var ret = new HsvColor
		{
			H = 60 * h1,
			S = saturation,
			V = max,
			A = TO_DOUBLE * color.A,
		};
		return ret;
	}

	/// <summary>
	/// Converts an sRGB-encoded color value to a linear one.
	/// </summary>
	/// <param name="srgb">An sRGB color value between 0.0 and 1.0.</param>
	/// <returns>A linearized value.</returns>
	private static double SRGBToLinear(double srgb)
	{
		if (srgb <= 0.04045)
			return srgb / 12.92;

		return Math.Pow((srgb + 0.055) / 1.055, 2.4);
	}

	/// <summary>
	/// Returns the luminance for a given color.
	/// </summary>
	/// <param name="c">A color object.</param>
	/// <returns>Luminance, in a range from 0.0 to 1.0.</returns>
	public static double GetLuminance(this Color c)
	{
		var linR = SRGBToLinear(c.R / 255d);
		var linG = SRGBToLinear(c.G / 255d);
		var linB = SRGBToLinear(c.B / 255d);

		return SRGB_COEFF_R * linR + SRGB_COEFF_G * linG + SRGB_COEFF_B * linB;
	}

	/// <summary>
	/// Returns a color's perceptual lightness.
	/// </summary>
	/// <param name="c">A color object.</param>
	/// <returns>Perceptual lightness in a range from 0.0 to 1.0.</returns>
	public static double GetLightness(this Color c)
	{
		var luminance = c.GetLuminance();

		if (luminance <= EPSILON)
			return luminance * KAPPA / 100;

		return (Math.Pow(luminance, THIRD) * 116 - 16) / 100;
	}
}
