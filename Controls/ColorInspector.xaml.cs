using System.Windows;
using System.Windows.Media;
using DeadEye.Helpers;

namespace DeadEye.Controls {
	public partial class ColorInspector {
		public ColorInspector() { this.InitializeComponent(); }

		public ColorWrapper ColorWrapper {
			get => (ColorWrapper)this.GetValue(ColorWrapperProperty);
			set => this.SetValue(ColorWrapperProperty, value);
		}

		public static readonly DependencyProperty ColorWrapperProperty = DependencyProperty.RegisterAttached(
			"ColorWrapper",
			typeof(ColorWrapper),
			typeof(ColorInspector),
			new FrameworkPropertyMetadata(new ColorWrapper("Magenta", Brushes.Magenta), FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));
	}
}