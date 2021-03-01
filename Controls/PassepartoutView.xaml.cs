using System.Windows;

namespace DeadEye.Controls {
	/// <summary>
	/// Interaction logic for PassepartoutView.xaml
	/// </summary>
	public partial class PassepartoutView {
		public PassepartoutView() {
			this.InitializeComponent();
		}

		public Rect CutoutRect {
			get => (Rect)this.GetValue(CutoutRectProperty);
			set => this.SetValue(CutoutRectProperty, value);
		}

		public static readonly DependencyProperty CutoutRectProperty = DependencyProperty.RegisterAttached(
			"CutoutRect",
			typeof(Rect),
			typeof(PassepartoutView),
			new FrameworkPropertyMetadata(new Rect(new Size(100, 100)), FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));
	}
}
