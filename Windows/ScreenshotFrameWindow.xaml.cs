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

public sealed class ScreenshotEventArgs : EventArgs
{
	public ScreenshotEventArgs(Int32Rect croppedRect)
	{
		this.CroppedRect = croppedRect;
	}

	public ScreenshotEventArgs(Color pickedColor)
	{
		this.PickedColor = pickedColor;
	}

	public Int32Rect? CroppedRect { get; private set; }
	public Color? PickedColor { get; private set; }
}

public delegate void ScreenshotTakenEventHandler(object sender, ScreenshotEventArgs args);

public enum ScreenshotFrameMode
{
	Screenshot,
	ColorPicker,
}

public sealed partial class ScreenshotFrameWindow : INotifyPropertyChanged
{
	private const double BOUNDS_DISPLAY_PADDING = 5;

	// copy of the virtual screen rect with the X and Y coordinates set to 0
	private readonly Rect _virtualScreenRectNormalized;

	private bool _isMakingSelection;
	private bool _isMovingSelection;

	private Point _cursorPosition, _colorPickerPosition;
	private Point _moveSelectionStart, _moveSelectionEnd, _moveSelectionOffset;
	private Point _selectionStartPoint, _selectionEndPoint;

	private ImageSource? _screenshotImage;

	private ScreenshotFrameMode _currentMode = ScreenshotFrameMode.Screenshot;

	public ScreenshotFrameWindow()
	{
		this.InitializeComponent();

		var virtualScreenRect = ScreenshotHelper.GetVirtualScreenRect();
		this.SetSize(virtualScreenRect);

		this._virtualScreenRectNormalized = ScreenshotHelper.GetVirtualScreenRectNormalized();
	}

	public ScreenshotFrameWindow(ref BitmapSource screenshotSource): this()
	{
		this.ScreenshotImage = screenshotSource;
	}

	public event ScreenshotTakenEventHandler? ScreenshotTaken;

	private void ScreenshotFrameWindow_OnSourceInitialized(object sender, EventArgs e)
	{
#if !DEBUG
			this.Topmost = true;
#endif
	}
	
	private void OnScreenshot(ScreenshotEventArgs e)
	{
		this.ScreenshotTaken?.Invoke(this, e);
	}

	private void ScreenshotFrameWindow_OnDeactivated(object sender, EventArgs e)
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

	public Point SelectionStartPoint
	{
		get => this._selectionStartPoint;
		private set
		{
			this._selectionStartPoint = value;
			this.OnPropertyChanged();
			this.OnPropertyChanged(nameof(this.SelectionBounds));
			this.OnPropertyChanged(nameof(this.SelectionBoundsClamped));
		}
	}

	public Point SelectionEndPoint
	{
		get => this._selectionEndPoint;
		private set
		{
			this._selectionEndPoint = value;
			this.OnPropertyChanged();
			this.OnPropertyChanged(nameof(this.SelectionBounds));
			this.OnPropertyChanged(nameof(this.SelectionBoundsClamped));
		}
	}

	public Rect SelectionBounds => new(this.SelectionStartPoint, this.SelectionEndPoint);
	public Rect SelectionBoundsClamped => Rect.Intersect(this.SelectionBounds, this._virtualScreenRectNormalized);

	/// <summary>
	/// The coordinates at which the BoundsDisplay thingy is displayed. Only used as a Binding in the XAML.
	/// </summary>
	public Point BoundsDisplayPosition
	{
		get
		{
			var boundsSize = new Size(this.BoundsDisplay.ActualWidth, this.BoundsDisplay.ActualHeight);

			// cursor is on the right edge of the selection rectangle
			var isRight = this._selectionEndPoint.X >= this._selectionStartPoint.X;

			// cursor is on the bottom edge of the selection rectangle
			var isBottom = this._selectionEndPoint.Y >= this._selectionStartPoint.Y;

			double newX, newY;
			if (isRight)
			{
				newX = this.SelectionBoundsClamped.X + this.SelectionBoundsClamped.Width;
				newX += BOUNDS_DISPLAY_PADDING;
			}
			else
			{
				newX = this.SelectionBoundsClamped.X - boundsSize.Width;
				newX -= BOUNDS_DISPLAY_PADDING;
			}

			if (isBottom)
			{
				newY = this.SelectionBoundsClamped.Y + this.SelectionBoundsClamped.Height;
				newY += BOUNDS_DISPLAY_PADDING;
			}
			else
			{
				newY = this.SelectionBoundsClamped.Y - boundsSize.Height;
				newY -= BOUNDS_DISPLAY_PADDING;
			}

			var newPoint = new Point(newX, newY);
			var newBounds = new Rect(newPoint, boundsSize);

			// make sure the size display never leaves the confines of the screen the cursor is on
			var activeScreen = Screen.FromNormalizedPoint(this._selectionEndPoint);
			var activeScreenBounds = activeScreen.BoundsNormalized;
			newPoint.X = Math.Clamp(newPoint.X, activeScreenBounds.Left, activeScreenBounds.Right - newBounds.Width);
			newPoint.Y = Math.Clamp(newPoint.Y, activeScreenBounds.Top, activeScreenBounds.Bottom - newBounds.Height);

			return newPoint;
		}
	}

	public bool IsMakingSelection
	{
		get => this._isMakingSelection;
		private set
		{
			this._isMakingSelection = value;
			this.OnPropertyChanged();
		}
	}

	public bool IsMovingSelection
	{
		get => this._isMovingSelection;
		private set
		{
			this._isMovingSelection = value;
			this.OnPropertyChanged();
		}
	}

	public ScreenshotFrameMode CurrentMode
	{
		get => this._currentMode;
		set
		{
			this._currentMode = value;
			this.OnPropertyChanged();
		}
	}

	#endregion

	#region ColorPicker support properties

	private double _colorPickerSize;

	public Point CursorPosition
	{
		get => this._cursorPosition;
		private set
		{
			this.OnPropertyChanged(nameof(this.BoundsDisplayPosition));

			if (this._colorPickerSize == 0 && this.ColorGizmo.ActualWidth > 0)
				this._colorPickerSize = this.ColorGizmo.ActualWidth;

			this._cursorPosition = value;
			this.OnPropertyChanged();
			value.X = (int)Math.Floor(value.X - this._colorPickerSize / 2);
			value.Y = (int)Math.Floor(value.Y - this._colorPickerSize / 2);
			this.ColorPickerPosition = value;
			this.UpdateCroppedImage();
		}
	}

	private void UpdateCroppedImage()
	{
		if (this.ScreenshotImage == null)
			return;

		this.ColorPickerCroppedBitmap = new CroppedBitmap((this.ScreenshotImage as BitmapSource)!, this.ColorPickerSourceRect);
		var offset = (int)Math.Floor((double)ColorPicker.SOURCE_RECT_SIZE / 2);
		var cb = new CroppedBitmap(this.ColorPickerCroppedBitmap, new Int32Rect(offset, offset, 1, 1));
		var pixels = new byte[4]; // bgra
		cb.CopyPixels(pixels, 4, 0);
		this.ColorPickerPixelColor = Color.FromArgb(255, pixels[2], pixels[1], pixels[0]);
		this.OnPropertyChanged(nameof(this.ColorPickerCroppedBitmap));
		this.OnPropertyChanged(nameof(this.ColorPickerPixelColor));
	}

	public Point ColorPickerPosition
	{
		get => this._colorPickerPosition;
		set
		{
			this._colorPickerPosition = value;
			this.OnPropertyChanged();
		}
	}

	public Int32Rect ColorPickerSourceRect
	{
		get
		{
			var pos = this.CursorPosition;
			pos.X -= Math.Floor((double)ColorPicker.SOURCE_RECT_SIZE / 2);
			pos.Y -= Math.Floor((double)ColorPicker.SOURCE_RECT_SIZE / 2);
			pos.X = Math.Clamp(pos.X, 0, this.ActualWidth);
			pos.Y = Math.Clamp(pos.Y, 0, this.ActualHeight);

			var rect = new Int32Rect((int)pos.X, (int)pos.Y, ColorPicker.SOURCE_RECT_SIZE, ColorPicker.SOURCE_RECT_SIZE);
			return rect;
		}
	}

	public Color ColorPickerPixelColor { get; private set; }

	public CroppedBitmap? ColorPickerCroppedBitmap { get; private set; }

	#endregion

	#region INotifyPropertyChanged

	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion

	#region Mouse and Key Handlers

	private void ScreenshotFrameWindow_OnKeyDown(object sender, KeyEventArgs e)
	{
		// stop if this is a repeat keypress
		if (e.IsRepeat)
			return;

		switch (e.Key)
		{
			case Key.Escape:
				if (this.IsMakingSelection)
					this.ResetDrawingFrame();
				else
					this.Close(); // Screenshot was cancelled by pressing Escape

				break;
			case Key.Space:
				if (!this.IsMakingSelection)
					return;

				this.IsMovingSelection = true;

				this._moveSelectionStart = this.SelectionStartPoint;
				this._moveSelectionEnd = this.SelectionEndPoint;
				this._moveSelectionOffset = this.SelectionEndPoint;

				break;
			case Key.C:
				// toggle color picker modes
				if (this.CurrentMode == ScreenshotFrameMode.Screenshot)
				{
					if (this.IsMakingSelection)
						this.ResetDrawingFrame();

					this.IsMakingSelection = false;
					this.IsMovingSelection = false;

					this.CurrentMode = ScreenshotFrameMode.ColorPicker;
				}
				else if (this.CurrentMode == ScreenshotFrameMode.ColorPicker)
					this.CurrentMode = ScreenshotFrameMode.Screenshot;

				break;
		}
	}

	private void ScreenshotFrameWindow_OnKeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Space)
			this.IsMovingSelection = false;
	}

	private void ScreenshotFrameWindow_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		var pos = e.GetPosition(this);
		this.StartDrawingFrame(pos);
	}

	private void ScreenshotFrameWindow_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (this.IsMakingSelection)
			this.ResetDrawingFrame();
		else
			this.Close(); // Screenshot was cancelled by pressing RMOUSE
	}

	private void ScreenshotFrameWindow_OnMouseMove(object sender, MouseEventArgs e)
	{
		var pos = e.GetPosition(this);
		this.CursorPosition = pos;

		if (!this.IsMakingSelection || this.CurrentMode != ScreenshotFrameMode.Screenshot || e.LeftButton != MouseButtonState.Pressed)
			return;

		if (this.IsMovingSelection)
			this.MoveDrawingFrame(pos);
		else
			this.ResizeDrawingFrame(pos);
	}

	private void ScreenshotFrameWindow_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		Debug.WriteLine("----- OnMouseLeftButtonUp");
		if (this.CurrentMode == ScreenshotFrameMode.Screenshot && !this.IsMakingSelection)
		{
			Debug.WriteLine("Selection already disabled");
			return;
		}

		var cursorPos = e.GetPosition(this);
		cursorPos.Offset(this.Left, this.Top);

		if (this.CurrentMode == ScreenshotFrameMode.ColorPicker)
		{
			// A pixel has been selected with the color picker. Fire event
			var colorEventArgs = new ScreenshotEventArgs(this.ColorPickerPixelColor);
			this.OnScreenshot(colorEventArgs);
			this.Close();
			return;
		}

		var cropRect = this.SelectionBounds;
		cropRect.Intersect(this._virtualScreenRectNormalized); // limit crop rect to the virtual screen bounds
		cropRect.Offset(this.Left, this.Top); // move crop rect into un-normalized virtual screen space

		//Debug.WriteLine(ScreenshotHelper.GetVirtualScreenRect());
		//Debug.WriteLine(this._virtualScreenRectNormalized);

		//Debug.WriteLine($"Window TopLeft: {this.Left};{this.Top}");
		//Debug.WriteLine($"Crop Rect: {cropRect}");
		//Debug.WriteLine($"Cursor: {cursorPos}");

		if (cropRect.Width == 0 || cropRect.Height == 0)
		{
			// Selected region is invalid. No event will be fired
			this.Close();
			return;
		}

		var (correctedX, correctedY) = (false, false);
		foreach (var screen in Screen.AllScreens)
		{
			if (!cropRect.IntersectsWith(screen.Bounds))
				continue;

			Debug.WriteLine($"Crop Rect intersects {screen.DeviceName}");

			if (!correctedX && (int)cropRect.Right == (int)screen.Bounds.Right - 1)
			{
				cropRect.Union(new Point(screen.Bounds.Right, cropRect.Bottom));
				correctedX = true;
				Debug.WriteLine("Width corrected");
			}

			if (!correctedY && (int)cropRect.Bottom == (int)screen.Bounds.Bottom - 1)
			{
				cropRect.Union(new Point(cropRect.Right, screen.Bounds.Bottom));
				correctedY = true;
				Debug.WriteLine("Height corrected");
			}
		}

		if (correctedX || correctedY)
			Debug.WriteLine($"Corrected Crop Rect: {cropRect}");

		// move crop rect back into normalized virtual screen space so it can be used to actually crop bitmaps
		cropRect.Offset(-this.Left, -this.Top);

		// A valid region has been selected. Fire event
		var eventArgs = new ScreenshotEventArgs(cropRect.ToInt32Rect());
		this.OnScreenshot(eventArgs);
		this.Close();
	}

	#endregion

	#region Frame drawing logic

	private void StartDrawingFrame(Point mousePosition)
	{
		this.ResetDrawingFrame();

		this.IsMakingSelection = true;
		this.SelectionStartPoint = mousePosition;
		this.SelectionEndPoint = mousePosition;
	}

	private void ResizeDrawingFrame(Point mousePosition)
	{
		this.SelectionEndPoint = mousePosition;
	}

	private void MoveDrawingFrame(Point mousePosition)
	{
		this._moveSelectionOffset = mousePosition;

		var moveDiff = this._moveSelectionOffset - this._moveSelectionEnd;
		this.SelectionStartPoint = this._moveSelectionStart + moveDiff;
		this.SelectionEndPoint = this._moveSelectionEnd + moveDiff;
	}

	private void ResetDrawingFrame()
	{
		this.IsMakingSelection = false;
		this.IsMovingSelection = false;

		// Hide crop rectangle
		var zeroPoint = new Point();
		this.SelectionStartPoint = zeroPoint;
		this.SelectionEndPoint = zeroPoint;
		this._moveSelectionStart = zeroPoint;
		this._moveSelectionEnd = zeroPoint;
		this._moveSelectionOffset = zeroPoint;
	}

	#endregion
}
