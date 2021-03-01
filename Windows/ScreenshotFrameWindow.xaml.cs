using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DeadEye.Extensions;
using DeadEye.Helpers;
using Image = System.Drawing.Image;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace DeadEye.Windows {
	/// <summary>
	/// Interaction logic for ScreenshotFrameWindow.xaml
	/// </summary>
	public sealed partial class ScreenshotFrameWindow: INotifyPropertyChanged {

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged([CallerMemberName] string propertyName = null) {
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private Image Screenshot {
			set => this.ScreenshotSource = value.ToBitmapImage();
		}

		private BitmapSource _screenshotSource;
		public BitmapSource ScreenshotSource {
			get => this._screenshotSource;
			private set {
				this._screenshotSource = value;
				this.OnPropertyChanged();
			}
		}

		public CroppedBitmap CroppedScreenshot { get; private set; }

		private Point _selectionStartPoint, _selectionEndPoint;
		public Point SelectionStartPoint {
			get => this._selectionStartPoint;
			private set {
				this._selectionStartPoint = value;
				this.OnPropertyChanged();
				this.OnPropertyChanged("SelectionBounds");
				this.OnPropertyChanged("SelectionBoundsClamped");
				this.OnPropertyChanged("BoundsDisplayCoords");
			}
		}
		public Point SelectionEndPoint {
			get => this._selectionEndPoint;
			private set {
				this._selectionEndPoint = value;
				this.OnPropertyChanged();
				this.OnPropertyChanged("SelectionBounds");
				this.OnPropertyChanged("SelectionBoundsClamped");
				this.OnPropertyChanged("BoundsDisplayCoords");
			}
		}
		public Rect SelectionBounds => new Rect(this.SelectionStartPoint, this.SelectionEndPoint);
		public Rect SelectionBoundsClamped => this.virtualScreenRectNormalized == Rect.Empty ? this.SelectionBounds : Rect.Intersect(this.SelectionBounds, this.virtualScreenRectNormalized);

		private const double BOUNDS_DISPLAY_PADDING = 4;
		public Point BoundsDisplayCoords {
			get {
				var boundsBounds = new Size(this.RectDisplay.ActualWidth, this.RectDisplay.ActualHeight);

				var isRight = this._selectionEndPoint.X >= this._selectionStartPoint.X;
				var isBottom = this._selectionEndPoint.Y >= this._selectionStartPoint.Y;

				double newX, newY;
				if (isRight) {
					newX = this.SelectionBoundsClamped.X + this.SelectionBoundsClamped.Width;
					newX += BOUNDS_DISPLAY_PADDING;
				} else {
					newX = this.SelectionBoundsClamped.X - boundsBounds.Width;
					newX -= BOUNDS_DISPLAY_PADDING;
				}

				if (isBottom) {
					newY = this.SelectionBoundsClamped.Y + this.SelectionBoundsClamped.Height;
					newY += BOUNDS_DISPLAY_PADDING;
				} else {
					newY = this.SelectionBoundsClamped.Y - boundsBounds.Height;
					newY -= BOUNDS_DISPLAY_PADDING;
				}

				return new Point(newX + BOUNDS_DISPLAY_PADDING, newY + BOUNDS_DISPLAY_PADDING);
			}
		}

		private bool _isMakingSelection;
		private bool _isMovingSelection;

		public bool IsMakingSelection {
			get => this._isMakingSelection;
			private set {
				this._isMakingSelection = value;
				this.OnPropertyChanged();
			}
		}
		public bool IsMovingSelection {
			get => this._isMovingSelection;
			private set {
				this._isMovingSelection = value;
				this.OnPropertyChanged();
			}
		}

		private Point moveSelectionStart;
		private Point moveSelectionEnd;
		private Point moveSelectionOffset;

		// copy of the virtual screen rect with the X coordinate set to 0
		private readonly Rect virtualScreenRectNormalized;

		public ScreenshotFrameWindow() {
			this.InitializeComponent();

			var virtualScreenRect = ScreenshotHelper.GetVirtualScreenRect();
			this.SetSize(virtualScreenRect);
			this.virtualScreenRectNormalized = virtualScreenRect;
			this.virtualScreenRectNormalized.X = 0;
		}

		public ScreenshotFrameWindow(Image screenshot): this() {
			this.Screenshot = screenshot;
		}

		private void ScreenshotFrameWindow_OnLoaded(object sender, RoutedEventArgs e) {
#if !DEBUG
				this.Topmost = true;
#endif

			if (!this.Activate()) {
				Debug.WriteLine("couldn't bring screenshot window to front");
			}
		}

		private void ScreenshotFrameWindow_OnLostFocus(object sender, RoutedEventArgs e) {
			Debug.WriteLine("lost focus");
			this.CloseDialog(false);
		}

		private void ScreenshotFrameWindow_OnKeyDown(object sender, KeyEventArgs e) {
			switch (e.Key) {
				case Key.Escape:
					if (this.IsMakingSelection) {
						this.IsMakingSelection = false;
						this.ResetDrawingFrame();
					} else {
						this.CloseDialog(false);
					}

					break;
				case Key.Space:
					// stop if this is a repeat keypress
					if (e.IsRepeat || !this.IsMakingSelection)
						return;

					this.IsMovingSelection = true;

					this.moveSelectionStart = this.SelectionStartPoint;
					this.moveSelectionEnd = this.SelectionEndPoint;
					this.moveSelectionOffset = this.SelectionEndPoint;

					break;
			}
		}

		private void ScreenshotFrameWindow_OnKeyUp(object sender, KeyEventArgs e) {
			if (e.Key == Key.Space) {
				this.IsMovingSelection = false;
			}
		}

		private void ScreenshotFrameWindow_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			var pos = e.GetPosition(this);
			this.StartDrawingFrame(pos);
		}

		private void ScreenshotFrameWindow_OnMouseMove(object sender, MouseEventArgs e) {
			if (e.LeftButton != MouseButtonState.Pressed || !this.IsMakingSelection)
				return;

			var pos = e.GetPosition(this);
			if (this.IsMovingSelection) {
				this.MoveDrawingFrame(pos);
			} else {
				this.ResizeDrawingFrame(pos);
			}
		}

		private void ScreenshotFrameWindow_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			if (!this.IsMakingSelection) {
				Debug.WriteLine("Selection already disabled");
				return;
			}

			var cropRect = this.SelectionBounds;
			cropRect.Intersect(this.virtualScreenRectNormalized);

			this.CloseDialog(true, new CroppedBitmap(this.ScreenshotSource, cropRect.ToInt32Rect()));
		}

		private void CloseDialog(bool result, CroppedBitmap screenshot = null) {
			this.CroppedScreenshot = screenshot;
			this.DialogResult = result;
			this.Close();
		}

		private void StartDrawingFrame(Point mousePosition) {
			this.ResetDrawingFrame();

			this.IsMakingSelection = true;
			this.SelectionStartPoint = mousePosition;
			this.SelectionEndPoint = mousePosition;
		}

		private void ResizeDrawingFrame(Point mousePosition) {
			this.SelectionEndPoint = mousePosition;
		}

		private void MoveDrawingFrame(Point mousePosition) {
			this.moveSelectionOffset = mousePosition;

			var moveDiff = this.moveSelectionOffset - this.moveSelectionEnd;
			this.SelectionStartPoint = this.moveSelectionStart + moveDiff;
			this.SelectionEndPoint = this.moveSelectionEnd + moveDiff;
		}

		private void ResetDrawingFrame() {
			this.IsMakingSelection = false;
			this.IsMovingSelection = false;

			// Hide crop rectangle
			var zeroPoint = new Point();
			this.SelectionStartPoint = zeroPoint;
			this.SelectionEndPoint = zeroPoint;
			this.moveSelectionStart = zeroPoint;
			this.moveSelectionEnd = zeroPoint;
			this.moveSelectionOffset = zeroPoint;
		}
	}
}
