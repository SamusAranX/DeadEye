using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DeadEye.Controls;
using DeadEye.Extensions;
using DeadEye.Helpers;

namespace DeadEye.Windows;

public sealed class ColorPickEventArgs : EventArgs
{
	public ColorPickEventArgs(Color pickedColor)
	{
		this.PickedColor = pickedColor;
	}

	public Color PickedColor { get; private set; }
}

public delegate void ColorPickedEventHandler(object sender, ColorPickEventArgs args);

public sealed partial class ColorPickerWindow : INotifyPropertyChanged
{
	private Point _colorPickerPosition;

	private ImageSource? _screenshotImage;

	public ColorPickerWindow()
	{
		this.InitializeComponent();

		var virtualScreenRect = ScreenshotHelper.GetVirtualScreenRect();
		this.SetSize(virtualScreenRect);
	}

	public ColorPickerWindow(ref BitmapSource screenshotSource) : this()
	{
		this.ScreenshotImage = screenshotSource;
	}

	private void ColorPickerWindow_OnSourceInitialized(object sender, EventArgs e)
	{
#if !DEBUG
			this.Topmost = true;
#endif
	}

	public event ColorPickedEventHandler? ColorPicked;
	private void OnColorPicked(ColorPickEventArgs e)
	{
		this.ColorPicked?.Invoke(this, e);
	}

	private void ColorPickerWindow_OnDeactivated(object sender, EventArgs e)
	{
		Debug.WriteLine("lost focus");

		if (!this.IsLoaded)
			this.Close();
	}

	#region Bound Properties and derivatives

	public ImageSource? ScreenshotImage
	{
		get => this._screenshotImage;
		set
		{
			this._screenshotImage = value;
			this.OnPropertyChanged();
		}
	}

	#endregion

	#region ColorPicker support properties

	public Point ColorPickerPosition
	{
		get => this._colorPickerPosition;
		set
		{
			this._colorPickerPosition = value;
			this.OnPropertyChanged();
		}
	}

	private Int32Rect _colorPickerSourceRect;
	private Color _colorPickerPixelColor;
	private CroppedBitmap? _colorPickerCroppedBitmap;

	public Color ColorPickerPixelColor
	{
		get => this._colorPickerPixelColor;
		private set
		{
			this._colorPickerPixelColor = value;
			this.OnPropertyChanged();
		}
	}

	public CroppedBitmap? ColorPickerCroppedBitmap
	{
		get => this._colorPickerCroppedBitmap;
		private set
		{
			this._colorPickerCroppedBitmap = value;
			this.OnPropertyChanged();
		}
	}

	#endregion

	#region INotifyPropertyChanged

	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion

	#region ColorPicker logic

	private double _colorPickerSize;

	private void UpdateColorPicker()
	{
		if (this.ScreenshotImage == null)
			return;

		this.ColorPickerCroppedBitmap = new CroppedBitmap((this.ScreenshotImage as BitmapSource)!, this._colorPickerSourceRect);
		var offset = (int)Math.Floor((double)ColorPicker.SOURCE_RECT_SIZE / 2);
		var cb = new CroppedBitmap(this.ColorPickerCroppedBitmap, new Int32Rect(offset, offset, 1, 1));
		var pixels = new byte[4]; // bgra
		cb.CopyPixels(pixels, 4, 0);
		this.ColorPickerPixelColor = Color.FromArgb(255, pixels[2], pixels[1], pixels[0]);
	}

	#endregion

	#region Mouse and Key Handlers

	private void ColorPickerWindow_OnKeyDown(object sender, KeyEventArgs e)
	{
		// stop if this is a repeat keypress
		if (e.IsRepeat)
			return;

		if (e.Key == Key.Escape)
			this.Close();
	}

	private void ColorPickerWindow_OnMouseMove(object sender, MouseEventArgs e)
	{
		if (this._colorPickerSize == 0 && this.ColorGizmo.ActualWidth > 0)
			this._colorPickerSize = this.ColorGizmo.ActualWidth;

		var pos = e.GetPosition(this);

		var pickerPosX = (int)Math.Floor(pos.X - this._colorPickerSize / 2);
		var pickerPosY = (int)Math.Floor(pos.Y - this._colorPickerSize / 2);
		this.ColorPickerPosition = new Point(pickerPosX, pickerPosY);

		var pickerPadding = (int)Math.Round((double)ColorPicker.SOURCE_RECT_SIZE / 2);
		pos.X -= pickerPadding;
		pos.Y -= pickerPadding;
		pos.X = Math.Clamp(pos.X, 0, this.ActualWidth - ColorPicker.SOURCE_RECT_SIZE);
		pos.Y = Math.Clamp(pos.Y, 0, this.ActualHeight - ColorPicker.SOURCE_RECT_SIZE);
		this._colorPickerSourceRect = new Int32Rect((int)pos.X, (int)pos.Y, ColorPicker.SOURCE_RECT_SIZE, ColorPicker.SOURCE_RECT_SIZE);
		//Debug.WriteLine(this._colorPickerSourceRect);

		this.UpdateColorPicker();
	}

	private void ColorPickerWindow_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		Debug.WriteLine("----- OnMouseLeftButtonDown");

		// A pixel has been selected with the color picker. Fire event
		var colorEventArgs = new ColorPickEventArgs(this.ColorPickerPixelColor);
		this.OnColorPicked(colorEventArgs);
		this.Close();
	}

	private void ColorPickerWindow_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
	{
		this.Close();
	}

	#endregion
}
