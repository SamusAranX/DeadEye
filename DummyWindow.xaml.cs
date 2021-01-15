using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using DeadEye.Hotkeys;

namespace DeadEye {
	/// <summary>
	/// Interaction logic for DummyWindow.xaml
	/// </summary>
	public partial class DummyWindow {
		[DllImport("user32.dll")]
		private static extern IntPtr SetParent(IntPtr hwnd, IntPtr hwndNewParent);
		private const int HWND_MESSAGE = -3;
		private IntPtr hwnd;

		private HotKey overlayHotkey;

		private void DummyWindow_OnSourceInitialized(object sender, EventArgs e) {
			// Make window message-only
			if (PresentationSource.FromVisual(this) is HwndSource hwndSource) {
				this.hwnd = hwndSource.Handle;
				SetParent(this.hwnd, (IntPtr)HWND_MESSAGE);
			}

			// Register hotkeys
			this.overlayHotkey = new HotKey(ModifierKeys.Control | ModifierKeys.Windows, Key.S, this, key => {
				Debug.WriteLine("Overlay Hotkey!");
			});
		}

		private void DummyWindow_OnClosed(object sender, EventArgs e) {
			this.overlayHotkey.Dispose();
		}

		public DummyWindow() { this.InitializeComponent(); }
	}
}