using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DeadEye.Controls {
	/// <summary>
	/// Interaction logic for GuideGrid.xaml
	/// </summary>
	public partial class GuideGrid : UserControl {
		public GuideGrid() { this.InitializeComponent(); }

		public Brush GridLineBrush {
			get => (Brush)this.GetValue(GridLineBrushProperty);
			set => this.SetValue(GridLineBrushProperty, value);
		}

		public Brush GridShadowBrush {
			get => (Brush)this.GetValue(GridShadowBrushProperty);
			set => this.SetValue(GridShadowBrushProperty, value);
		}

		public GridType GridType {
			get => (GridType)this.GetValue(GridTypeProperty);
			set => this.SetValue(GridTypeProperty, value);
		}

		public static readonly DependencyProperty GridLineBrushProperty = DependencyProperty.RegisterAttached(
			"GridLineBrush",
			typeof(Brush),
			typeof(GuideGrid),
			new FrameworkPropertyMetadata(new SolidColorBrush(Colors.White), FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

		public static readonly DependencyProperty GridShadowBrushProperty = DependencyProperty.RegisterAttached(
			"GridShadowBrush",
			typeof(Brush),
			typeof(GuideGrid),
			new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(136,0,0,0)), FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

		public static readonly DependencyProperty GridTypeProperty = DependencyProperty.RegisterAttached(
			"GridType",
			typeof(GridType),
			typeof(GuideGrid),
			new FrameworkPropertyMetadata(GridType.None));
	}
}
