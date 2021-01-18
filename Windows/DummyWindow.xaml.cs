using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using DeadEye.HotKeys;
using DeadEye.NotifyIcon;

namespace DeadEye.Windows {
	/// <summary>
	/// Interaction logic for DummyWindow.xaml
	/// </summary>
	public partial class DummyWindow {
		private HotKey overlayHotkey;

		private ScreenshotFrameWindow screenshotWindow;
		private SettingsWindow settingsWindow;
		private AboutWindow aboutWindow;

		#region Initialization and Shutdown

		[DllImport("user32.dll")]
		private static extern IntPtr SetParent(IntPtr hwnd, IntPtr hwndNewParent);

		private void DummyWindow_OnSourceInitialized(object sender, EventArgs e) {
			// Make screenshotWindow message-only
			const int HWND_MESSAGE = -3;
			if (PresentationSource.FromVisual(this) is HwndSource hwndSource) {
				SetParent(hwndSource.Handle, (IntPtr)HWND_MESSAGE);
			}

			// boop taskbar icon so it shows up
			var taskbarIcon = this.FindResource("TaskbarIcon") as TaskbarIcon;

			// Register hotkeys
			this.overlayHotkey = new HotKey(ModifierKeys.Alt | ModifierKeys.Shift, Key.S, this, this.OverlayHotkeyAction);
		}

		private void DummyWindow_OnClosed(object sender, EventArgs e) {
			this.overlayHotkey.Dispose();
		}

		#endregion

		#region Hotkey Actions

		private void OverlayHotkeyAction(HotKey key) {
			Debug.WriteLine("Overlay Hotkey!");

			if (this.screenshotWindow != null) {
				Debug.WriteLine("Window is already open");
				return;
			}

			var bm = Helpers.GetFullscreenScreenshotGDI();
			this.screenshotWindow = new ScreenshotFrameWindow(bm);
			var result = this.screenshotWindow.ShowDialog();

			if (result.HasValue && result.Value) {
				Debug.WriteLine("Screenshot taken.");
				var cropped = this.screenshotWindow.CroppedScreenshot;
				Clipboard.SetImage(cropped);
				Debug.WriteLine($"Image of size {cropped.PixelWidth}x{cropped.PixelHeight} saved to clipboard");
			} else {
				Debug.WriteLine("Screenshot cancelled.");
			}

			/*
			 * I'm explicitly setting screenshotWindow to null instead of relying on the Closed event handler
			 * because the event handler will happen before I can retrieve the screenshot
			 */ 
			this.screenshotWindow = null;
		}

		#endregion

		#region Context Menu Actions

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
			Application.Current.Shutdown();
		}

		#endregion
	}
}