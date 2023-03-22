using System.Windows.Media;
using DeadEye.Extensions;

namespace DeadEye.Helpers;

public sealed class ColorWrapper
{
	public ColorWrapper(string name, Brush brush)
	{
		this.Name = name;
		this.Brush = brush;

		var col = this.Color;
		var hsv = col.ToHsv();

		this.Red = col.R;
		this.Green = col.G;
		this.Blue = col.B;
		this.Alpha = col.A;

		this.Hue = hsv.H;
		this.Saturation = hsv.S;
		this.Value = hsv.V;

		this.Luma = col.GetLuma();
	}

	public string Name { get; }
	public Brush Brush { get; }

	public int Red { get; }
	public int Green { get; }
	public int Blue { get; }
	public int Alpha { get; }

	public double Hue { get; }
	public double Saturation { get; }
	public double Value { get; }

	public double Luma { get; }

	public Color Color
	{
		get
		{
			if (this.Brush is SolidColorBrush brush)
				return brush.Color;

			return Colors.Magenta;
		}
	}
}
