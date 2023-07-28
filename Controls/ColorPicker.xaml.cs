using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace DeadEye.Controls;

public partial class ColorPicker : INotifyPropertyChanged
{
	public const int IMAGE_SOURCE_RECT_SIZE = 15;

	public static readonly DependencyProperty PixelColorProperty = DependencyProperty.Register(
		nameof(PixelColor),
		typeof(Color),
		typeof(ColorPicker),
		new FrameworkPropertyMetadata(Colors.Magenta, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(
		nameof(ImageSource),
		typeof(ImageSource),
		typeof(ColorPicker),
		new FrameworkPropertyMetadata(default(ImageSource), FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty PickerRadiusProperty = DependencyProperty.Register(
		nameof(PickerRadius),
		typeof(double),
		typeof(ColorPicker),
		new FrameworkPropertyMetadata((double)72, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender, OnPickerRadiusChanged));

	private static void OnPickerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not ColorPicker picker)
			return;

		if (e.Property != PickerRadiusProperty)
			return;

		picker.PickerRadius2 = Math.Max(0, (double)e.NewValue - 1);
		picker.PickerRadius3 = Math.Max(0, (double)e.NewValue - 4);

		//Debug.WriteLine("{0} {1} {2}", picker.PickerRadius, picker.PickerRadius2, picker.PickerRadius3);
	}

	#region INotifyPropertyChanged

	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion

	public ColorPicker()
	{
		this.InitializeComponent();

		this.PickerRadius2 = Math.Max(0, this.PickerRadius - 1);
		this.PickerRadius3 = Math.Max(0, this.PickerRadius - 4);

		//Debug.WriteLine("{0} {1} {2}", this.PickerRadius, this.PickerRadius2, this.PickerRadius3);
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

	public double PickerRadius
	{
		get => (double)this.GetValue(PickerRadiusProperty);
		set => this.SetValue(PickerRadiusProperty, value);
	}

	private double _pickerRadius2, _pickerRadius3;

	public double PickerRadius2
	{
		get => this._pickerRadius2;
		private set
		{
			this._pickerRadius2 = value;
			this.OnPropertyChanged();
		}
	}

	public double PickerRadius3
	{
		get => this._pickerRadius3;
		private set
		{
			this._pickerRadius3 = value;
			this.OnPropertyChanged();
		}
	}
}
