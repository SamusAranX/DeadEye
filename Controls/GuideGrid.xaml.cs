using System.Windows;
using System.Windows.Media;

namespace DeadEye.Controls {
	/// <summary>
	/// Interaction logic for GuideGrid.xaml
	/// </summary>
	public partial class GuideGrid {
		public GuideGrid() { this.InitializeComponent(); }

		#region Dependency Properties

		public double GridOpacity {
			get => (double)this.GetValue(GridOpacityProperty);
			set => this.SetValue(GridOpacityProperty, value);
		}

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

		public bool MarkCenter {
			get => (bool)this.GetValue(MarkCenterProperty);
			set => this.SetValue(MarkCenterProperty, value);
		}

		public static readonly DependencyProperty GridOpacityProperty = DependencyProperty.RegisterAttached(
			"GridOpacity",
			typeof(double),
			typeof(GuideGrid),
			new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

		public static readonly DependencyProperty GridLineBrushProperty = DependencyProperty.RegisterAttached(
			"GridLineBrush",
			typeof(Brush),
			typeof(GuideGrid),
			new FrameworkPropertyMetadata(new SolidColorBrush(Colors.White), FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

		public static readonly DependencyProperty GridShadowBrushProperty = DependencyProperty.RegisterAttached(
			"GridShadowBrush",
			typeof(Brush),
			typeof(GuideGrid),
			new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(136, 0, 0, 0)), FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

		public static readonly DependencyProperty GridTypeProperty = DependencyProperty.RegisterAttached(
			"GridType",
			typeof(GridType),
			typeof(GuideGrid),
			new FrameworkPropertyMetadata(GridType.None, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

		public static readonly DependencyProperty MarkCenterProperty = DependencyProperty.RegisterAttached(
			"MarkCenter",
			typeof(bool),
			typeof(GuideGrid),
			new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

		#endregion

	}
}