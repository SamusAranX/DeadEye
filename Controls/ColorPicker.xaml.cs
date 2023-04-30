using System.Windows;
using System.Windows.Media;

namespace DeadEye.Controls;

public partial class ColorPicker
{
	public static readonly DependencyProperty InspectColorProperty = DependencyProperty.RegisterAttached(
		"InspectColor",
		typeof(Color),
		typeof(ColorPicker),
		new FrameworkPropertyMetadata(Colors.Magenta, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty MagnifyImageProperty = DependencyProperty.RegisterAttached(
		"MagnifyImage",
		typeof(ImageSource),
		typeof(ColorPicker),
		new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

	public ColorPicker()
	{
		this.InitializeComponent();
	}

	public Color InspectColor
	{
		get => (Color)this.GetValue(InspectColorProperty);
		set => this.SetValue(InspectColorProperty, value);
	}

	// TODO: figure out how to pass CroppedBitmap into this control and move the crop rect around
	public ImageSource MagnifyImage
	{
		get => (ImageSource)this.GetValue(MagnifyImageProperty);
		set => this.SetValue(MagnifyImageProperty, value);
	}
}
