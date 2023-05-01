using System.Windows;

namespace DeadEye.Controls;

public partial class BoundsDisplay
{
	public static readonly DependencyProperty DisplayRectProperty = DependencyProperty.RegisterAttached(
		"DisplayRect",
		typeof(Rect),
		typeof(BoundsDisplay),
		new FrameworkPropertyMetadata(new Rect(), FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

	public BoundsDisplay()
	{
		this.InitializeComponent();
	}

	public Rect DisplayRect
	{
		get => (Rect)this.GetValue(DisplayRectProperty);
		set => this.SetValue(DisplayRectProperty, value);
	}
}
