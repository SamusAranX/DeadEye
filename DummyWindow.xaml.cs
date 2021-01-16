using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using DeadEye.Extensions;
using DeadEye.HotKeys;
using DeadEye.NotifyIcon;

namespace DeadEye {
	/// <summary>
	/// Interaction logic for DummyWindow.xaml
	/// </summary>
	public partial class DummyWindow {
		private TaskbarIcon taskbarIcon;

		private HotKey overlayHotkey;
		private ScreenshotFrameWindow window;

		#region Initialization and Shutdown

		[DllImport("user32.dll")]
		private static extern IntPtr SetParent(IntPtr hwnd, IntPtr hwndNewParent);

		private void DummyWindow_OnSourceInitialized(object sender, EventArgs e) {
			// Make window message-only
			const int HWND_MESSAGE = -3;
			if (PresentationSource.FromVisual(this) is HwndSource hwndSource) {
				SetParent(hwndSource.Handle, (IntPtr)HWND_MESSAGE);
			}

			// boop taskbar icon so it shows up
			this.taskbarIcon = this.FindResource("TaskbarIcon") as TaskbarIcon;

			// Register hotkeys
			this.overlayHotkey = new HotKey(ModifierKeys.Alt | ModifierKeys.Shift, Key.S, this, this.OverlayHotkeyAction);
		}

		private void DummyWindow_OnClosed(object sender, EventArgs e) {
			this.overlayHotkey.Dispose();
			Debug.WriteLine("Dummy closing");
		}

		#endregion

		#region Hotkey Actions

		private void OverlayHotkeyAction(HotKey key) {
			Debug.WriteLine("Overlay Hotkey!");

			if (this.window != null) {
				Debug.WriteLine("Window is already open");
				return;
			}

			var bm = Helpers.GetFullscreenScreenshotGDI();
			this.window = new ScreenshotFrameWindow(bm);
			var result = this.window.ShowDialog();

			if (result.HasValue && result.Value) {
				Debug.WriteLine("Screenshot taken.");
				var cropped = this.window.CroppedScreenshot;
				Clipboard.SetImage(cropped);
				Debug.WriteLine($"Image of size {cropped.PixelWidth}x{cropped.PixelHeight} saved to clipboard");
			} else {
				Debug.WriteLine("Screenshot cancelled.");
			}

			this.window = null;
		}

		#endregion

		#region Context Menu Actions

		private void SettingsMenuItem_OnClick(object sender, RoutedEventArgs e) {
			Application.Current.Shutdown();
		}

		private void AboutMenuItem_OnClick(object sender, RoutedEventArgs e) {
			Application.Current.Shutdown();
		}

		private void ExitMenuItem_OnClick(object sender, RoutedEventArgs e) {
			Application.Current.Shutdown();
		}

		#endregion
	}
}