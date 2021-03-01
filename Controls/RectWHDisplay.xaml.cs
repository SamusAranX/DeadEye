using System.Windows;

namespace DeadEye.Controls {
	/// <summary>
	/// Interaction logic for RectWHDisplay.xaml
	/// </summary>
	public partial class RectWHDisplay {
		public RectWHDisplay() { this.InitializeComponent(); }

		public Rect DisplayRect {
			get => (Rect)this.GetValue(DisplayRectProperty);
			set => this.SetValue(DisplayRectProperty, value);
		}

		public static readonly DependencyProperty DisplayRectProperty = DependencyProperty.RegisterAttached(
			"DisplayRect",
			typeof(Rect),
			typeof(RectWHDisplay),
			new FrameworkPropertyMetadata(new Rect(), FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));
	}
}
