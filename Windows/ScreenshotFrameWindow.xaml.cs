using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DeadEye.Extensions;
using DeadEye.Helpers;

namespace DeadEye.Windows;

public sealed class ScreenshotEventArgs : EventArgs
{
	public ScreenshotEventArgs(CroppedBitmap screenshot)
	{
		this.Screenshot = screenshot;
	}

	public CroppedBitmap Screenshot { get; private set; }
}

public delegate void ScreenshotTakenEventHandler(object sender, ScreenshotEventArgs args);

public sealed partial class ScreenshotFrameWindow : INotifyPropertyChanged
{
	private const double BOUNDS_DISPLAY_PADDING = 5;

	// copy of the virtual screen rect with the X and Y coordinates set to 0
	private readonly Rect _virtualScreenRectNormalized;

	private bool _isMakingSelection;
	private bool _isMovingSelection;

	private Point _cursorPosition;
	private Point _moveSelectionStart, _moveSelectionEnd, _moveSelectionOffset;
	private Point _selectionStartPoint, _selectionEndPoint;

	private ImageSource? _screenshotImage;

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
	
	private void OnScreenshot(ScreenshotEventArgs e)
	{
		this.ScreenshotTaken?.Invoke(this, e);
	}

	private void ScreenshotFrameWindow_OnSourceInitialized(object sender, EventArgs e)
	{
		this.Topmost = !DebugHelper.IsDebugMode;
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

	#endregion

	public Point CursorPosition
	{
		get => this._cursorPosition;
		private set
		{
			this._cursorPosition = value;
			this.OnPropertyChanged();
			this.OnPropertyChanged(nameof(this.BoundsDisplayPosition));
		}
	}

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

		if (!this.IsMakingSelection || e.LeftButton != MouseButtonState.Pressed)
			return;

		if (this.IsMovingSelection)
			this.MoveDrawingFrame(pos);
		else
			this.ResizeDrawingFrame(pos);
	}

	private void ScreenshotFrameWindow_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		Debug.WriteLine("----- OnMouseLeftButtonUp");
		if (!this.IsMakingSelection)
		{
			Debug.WriteLine("Selection already disabled");
			return;
		}

		var cursorPos = e.GetPosition(this);
		cursorPos.Offset(this.Left, this.Top);

		var cropRect = this.SelectionBounds;
		cropRect.Intersect(this._virtualScreenRectNormalized); // limit crop rect to the virtual screen bounds
		cropRect.Offset(this.Left, this.Top); // move crop rect into un-normalized virtual screen space

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
		var croppedBitmap = new CroppedBitmap((this.ScreenshotImage as BitmapSource)!, cropRect.ToInt32Rect());
		croppedBitmap.Freeze();

		var eventArgs = new ScreenshotEventArgs(croppedBitmap);
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
