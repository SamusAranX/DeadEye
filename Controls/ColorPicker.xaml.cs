using System.Windows;
using System.Windows.Media;

namespace DeadEye.Controls;

public partial class ColorPicker
{
	public const int SOURCE_RECT_SIZE = 15;

	public static readonly DependencyProperty PixelColorProperty = DependencyProperty.RegisterAttached(
		"PixelColor",
		typeof(Color),
		typeof(ColorPicker),
		new FrameworkPropertyMetadata(Colors.Magenta, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.RegisterAttached(
		"ImageSource",
		typeof(ImageSource),
		typeof(ColorPicker),
		new FrameworkPropertyMetadata(default(ImageSource), FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

	public ColorPicker()
	{
		this.InitializeComponent();
	}

	public Color PixelColor
	{
		get => (Color)this.GetValue(PixelColorProperty);
		set => this.SetValue(PixelColorProperty, value);
	}
	 
	public ImageSource ImageSource
	{
		get => (ImageSource)this.GetValue(ImageSourceProperty);
		set => this.SetValue(ImageSourceProperty, value);
	}
}
