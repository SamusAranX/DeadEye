using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DeadEye.Extensions;
using DeadEye.Helpers;
using Image = System.Drawing.Image;

namespace DeadEye.Windows {
	/// <summary>
	/// Interaction logic for ScreenshotFrameWindow.xaml
	/// </summary>
	public sealed partial class ScreenshotFrameWindow: INotifyPropertyChanged {

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string propertyName = null) {
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
			}
		}
		public Point SelectionEndPoint {
			get => this._selectionEndPoint;
			private set {
				this._selectionEndPoint = value;
				this.OnPropertyChanged();
				this.OnPropertyChanged("SelectionBounds");
				this.OnPropertyChanged("SelectionBoundsClamped");
			}
		}
		public Rect SelectionBounds => new Rect(this.SelectionStartPoint, this.SelectionEndPoint);
		public Rect SelectionBoundsClamped => this.virtualScreenRectNormalized == Rect.Empty ? this.SelectionBounds : Rect.Intersect(this.SelectionBounds, this.virtualScreenRectNormalized);

		public bool IsMakingSelection { get; private set; }
		public bool IsMovingSelection { get; private set; }

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
			var selectionThreshold = Settings.SharedSettings.ThresholdSize;
			if (cropRect.Width <= selectionThreshold && cropRect.Height <= selectionThreshold)
				return;

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
