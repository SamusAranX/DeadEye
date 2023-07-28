using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace DeadEye.Controls;

public partial class ColorPicker : INotifyPropertyChanged
{
	public const int SOURCE_RECT_SIZE = 15;

	public static readonly DependencyProperty PixelColorProperty = DependencyProperty.RegisterAttached(
		"PixelColor",
		typeof(Color),
		typeof(ColorPicker),
		new FrameworkPropertyMetadata(Colors.Magenta, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.RegisterAttached(
		"ImageSource",
		typeof(ImageSource),
		typeof(ColorPicker),
		new FrameworkPropertyMetadata(default(ImageSource), FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty PickerTypeProperty = DependencyProperty.RegisterAttached(
		"PickerType",
		typeof(PickerType),
		typeof(ColorPicker),
		new FrameworkPropertyMetadata(PickerType.Circle, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

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

		this.UpdateCornerRadius();
	}

	private void UpdateCornerRadius()
	{
		switch (this.PickerType)
		{
			case PickerType.Circle:
				this.CornerRadius = 180;
				break;
			case PickerType.Square:
				this.CornerRadius = 0;
				break;
		}
	}

	private double _cornerRadius;

	public double CornerRadius
	{
		get => this._cornerRadius;
		set
		{
			this._cornerRadius = value;
			this.OnPropertyChanged();
			Debug.WriteLine("Updated CornerRadius to {0}", this._cornerRadius);
		}
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

	public PickerType PickerType
	{
		get => (PickerType)this.GetValue(PickerTypeProperty);
		set
		{
			this.SetValue(PickerTypeProperty, value);
			this.UpdateCornerRadius();
		}
	}
}
