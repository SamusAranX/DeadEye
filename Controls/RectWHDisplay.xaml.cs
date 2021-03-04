using System.Windows;

namespace DeadEye.Controls {
	public partial class RectWHDisplay {
		public static readonly DependencyProperty DisplayRectProperty = DependencyProperty.RegisterAttached(
			"DisplayRect",
			typeof(Rect),
			typeof(RectWHDisplay),
			new FrameworkPropertyMetadata(new Rect(), FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

		public RectWHDisplay() {
			this.InitializeComponent();
		}

		public Rect DisplayRect {
			get => (Rect)this.GetValue(DisplayRectProperty);
			set => this.SetValue(DisplayRectProperty, value);
		}
	}
}