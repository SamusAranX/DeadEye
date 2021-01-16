using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using DeadEye.Extensions;
using Image = System.Drawing.Image;

namespace DeadEye {
	/// <summary>
	/// Interaction logic for ScreenshotFrameWindow.xaml
	/// </summary>
	public sealed partial class ScreenshotFrameWindow: INotifyPropertyChanged {

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged([CallerMemberName] string propertyName = null) {
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public Image Screenshot {
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

		private Point _selectionStartPoint, _selectionEndPoint;
		public Point SelectionStartPoint {
			get => this._selectionStartPoint;
			private set {
				this._selectionStartPoint = value;
				this.OnPropertyChanged();
				this.OnPropertyChanged("SelectionBounds");
				this.OnPropertyChanged("SelectionRight");
				this.OnPropertyChanged("SelectionBottom");
			}
		}
		public Point SelectionEndPoint {
			get => this._selectionEndPoint;
			private set {
				this._selectionEndPoint = value;
				this.OnPropertyChanged();
				this.OnPropertyChanged("SelectionBounds");
				this.OnPropertyChanged("SelectionRight");
				this.OnPropertyChanged("SelectionBottom");
			}
		}
		public Rect SelectionBounds => new Rect(this.SelectionStartPoint, this.SelectionEndPoint);

		public double SelectionRight  => this.Width  - this.SelectionStartPoint.X - this.SelectionBounds.Width;
		public double SelectionBottom => this.Height - this.SelectionStartPoint.Y - this.SelectionBounds.Height;

		public bool DragSelectionActive { get; set; }
		public bool MoveSelectionActive { get; set; }
		public bool WindowSelectionEnabled { get; set; }

		private Point moveSelectionStart;
		private Point moveSelectionEnd;
		private Point moveSelectionOffset;

		public CroppedBitmap CroppedScreenshot { get; private set; }

		public ScreenshotFrameWindow() { 
			this.InitializeComponent();
			this.SetSize(Helpers.GetVirtualScreenRect());
		}

		public ScreenshotFrameWindow(Image screenshot): this() {
			this.Screenshot = screenshot;
			//this.ScreenshotSource = screenshot.ToBitmapImage();
		}

		private void ScreenshotFrameWindow_OnLoaded(object sender, RoutedEventArgs e) {
			this.Activate();
			this.Topmost = true;
		}

		private void ScreenshotFrameWindow_OnContentRendered(object sender, EventArgs e) {
			var sb = this.FindResource("FadeInStoryboard") as Storyboard;
			sb?.Begin();
		}

		private void ScreenshotFrameWindow_OnKeyDown(object sender, KeyEventArgs e) {
			// stop if this is a repeat keypress
			if (e.IsRepeat)
				return;

			if (e.Key == Key.Escape) {
				Debug.WriteLine($"ESC is dragging: {this.DragSelectionActive}");
				Debug.WriteLine($"ESC is moving: {this.MoveSelectionActive}");
				if (this.DragSelectionActive) {
					this.DragSelectionActive = false;
				} else {
					this.DialogResult = false;
					this.Close();
				}
			} else if (e.Key == Key.Space) {
				if (this.DragSelectionActive) {
					this.MoveSelectionActive = true;

					this.moveSelectionStart  = this.SelectionStartPoint;
					this.moveSelectionEnd    = this.SelectionEndPoint;
					this.moveSelectionOffset = this.SelectionEndPoint;
				} else {
					this.WindowSelectionEnabled = !this.WindowSelectionEnabled;
					Debug.WriteLine($"Window selection: {this.WindowSelectionEnabled}");
				}
			}
		}

		private void ScreenshotFrameWindow_OnKeyUp(object sender, KeyEventArgs e) {
			if (e.Key == Key.Space) {
				this.MoveSelectionActive = false;
			}
		}

		private void ScreenshotFrameWindow_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			this.DragSelectionActive = true;

			var pos = e.GetPosition(this);
			this.SelectionStartPoint = pos;
			this.SelectionEndPoint   = pos;
		}

		private void ScreenshotFrameWindow_OnMouseMove(object sender, MouseEventArgs e) {
			if (e.LeftButton != MouseButtonState.Pressed || !this.DragSelectionActive)
				return;

			var pos = e.GetPosition(this);
			this.SelectionEndPoint = pos;

			if (this.MoveSelectionActive) {
				this.moveSelectionOffset = Mouse.GetPosition(this);

				var moveDiff       = this.moveSelectionOffset - this.moveSelectionEnd;
				this.SelectionStartPoint = this.moveSelectionStart + moveDiff;
				this.SelectionEndPoint   = this.moveSelectionEnd + moveDiff;
			}
		}

		private void ScreenshotFrameWindow_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			this.DragSelectionActive = false;
			this.MoveSelectionActive = false;

			var pos = e.GetPosition(this);
			this.SelectionEndPoint = pos;

			var cropRect = this.SelectionBounds;

			// Hide crop rectangle
			var zeroPoint = new Point();
			this.SelectionStartPoint = zeroPoint;
			this.SelectionEndPoint   = zeroPoint;
			this.moveSelectionStart  = zeroPoint;
			this.moveSelectionEnd    = zeroPoint;
			this.moveSelectionOffset = zeroPoint;

			Debug.WriteLine($"is dragging: {this.DragSelectionActive}");
			Debug.WriteLine($"is moving: {this.MoveSelectionActive}");

			if (cropRect.Width <= 8 && cropRect.Height <= 8)
				return;

			Debug.WriteLine($"selection: {cropRect}");

			this.CroppedScreenshot = new CroppedBitmap(this.ScreenshotSource, cropRect.ToInt32Rect());
			this.DialogResult = true;
			this.Close();
		}
	}
}
