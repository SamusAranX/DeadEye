using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DeadEye.Extensions;
using DeadEye.Helpers;
using DeadEye.HotKeys;
using DeadEye.NotifyIcon;

namespace DeadEye.Windows {
	public partial class DummyWindow: IDisposable {
		private AboutWindow aboutWindow;
		private ColorBrowserWindow colorBrowserWindow;
		private HotKey overlayHotkey;
		private ScreenshotFrameWindow screenshotWindow;
		private SettingsWindow settingsWindow;

		#region Hotkey Actions

		private void OverlayHotkeyAction(HotKey key) {
			Debug.WriteLine("Overlay Hotkey!");

			if (this.screenshotWindow != null) {
				Debug.WriteLine("Window is already open");
				return;
			}

			using var bm = ScreenshotHelper.GetFullscreenScreenshotGDI();
			var bitmapImage = bm.ToBitmapSource();

			this.screenshotWindow = new ScreenshotFrameWindow(bitmapImage);
			this.screenshotWindow.ScreenshotTaken += (sender, args) => {
				Debug.WriteLine("Screenshot taken.");
				var croppedBitmap = new CroppedBitmap(bitmapImage, args.CroppedRect);

				try {
					Clipboard.SetImage(croppedBitmap);
				} catch (COMException e) {
					this.TaskbarIcon.ShowBalloonTip("Clipboard Error", $"Can't copy image into the Clipboard.\nTo fix this, click here to open the Settings and click \"Clear clipboard data\".\nMore Info: 0x{e.HResult:X8}", BalloonIcon.Error);
				}
				
				bitmapImage = null;
			};
			this.screenshotWindow.Closed += (sender, args) => {
				this.screenshotWindow = null;
			};

			this.screenshotWindow.Show();
		}

		private void TaskbarIconOnTrayBalloonTipClicked(object sender, RoutedEventArgs e) {
			Process.Start("ms-settings:clipboard");
		}

		#endregion

		#region Initialization and Shutdown

		[DllImport("user32.dll")]
		private static extern IntPtr SetParent(IntPtr hwnd, IntPtr hwndNewParent);

		private void DummyWindow_OnSourceInitialized(object sender, EventArgs e) {
			// Make screenshotWindow message-only
			const int HWND_MESSAGE = -3;
			if (PresentationSource.FromVisual(this) is HwndSource hwndSource) SetParent(hwndSource.Handle, (IntPtr)HWND_MESSAGE);

			// Register hotkeys
			this.overlayHotkey = new HotKey(ModifierKeys.Alt | ModifierKeys.Shift, Key.S, this, this.OverlayHotkeyAction);

			this.TaskbarIcon.TrayBalloonTipClicked += this.TaskbarIconOnTrayBalloonTipClicked;
		}

		public void Dispose() {
			this.overlayHotkey.Dispose();
			GC.SuppressFinalize(this);
		}

		#endregion

		#region Context Menu Actions

		private void ColorMenuItem_OnClick(object sender, RoutedEventArgs e) {
			if (this.colorBrowserWindow != null) {
				this.colorBrowserWindow.Activate();
				return;
			}

			this.colorBrowserWindow = new ColorBrowserWindow();
			this.colorBrowserWindow.Closed += (a, b) => this.colorBrowserWindow = null;
			this.colorBrowserWindow.Show();
		}

		private void AppDirMenuItem_OnClick(object sender, RoutedEventArgs e) {
			var appLocation = Assembly.GetExecutingAssembly().Location;
			var appPath = Path.GetDirectoryName(appLocation);

			Process.Start("explorer.exe", appPath);
		}
		
		private void GarbageMenuItem_OnClick(object sender, RoutedEventArgs e) {
			GCHelper.CleanUp();
		}

		private void SettingsMenuItem_OnClick(object sender, RoutedEventArgs e) {
			if (this.settingsWindow != null) {
				this.settingsWindow.Activate();
				return;
			}

			this.settingsWindow = new SettingsWindow();
			this.settingsWindow.Closed += (a, b) => this.settingsWindow = null;
			this.settingsWindow.Show();
		}

		private void AboutMenuItem_OnClick(object sender, RoutedEventArgs e) {
			if (this.aboutWindow != null) {
				this.aboutWindow.Activate();
				return;
			}

			this.aboutWindow = new AboutWindow();
			this.aboutWindow.Closed += (a, b) => this.aboutWindow = null;
			this.aboutWindow.Show();
		}

		private void ExitMenuItem_OnClick(object sender, RoutedEventArgs e) {
			Settings.SharedSettings.Save();
			Application.Current.Shutdown();
		}

		#endregion
	}
}