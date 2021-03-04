using System.Windows;

namespace DeadEye.Controls {
	public partial class PassepartoutView {
		public static readonly DependencyProperty CutoutRectProperty = DependencyProperty.RegisterAttached(
			"CutoutRect",
			typeof(Rect),
			typeof(PassepartoutView),
			new FrameworkPropertyMetadata(new Rect(new Size(100, 100)), FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

		public PassepartoutView() {
			this.InitializeComponent();
		}

		public Rect CutoutRect {
			get => (Rect)this.GetValue(CutoutRectProperty);
			set => this.SetValue(CutoutRectProperty, value);
		}
	}
}