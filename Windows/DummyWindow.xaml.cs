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
using DeadEye.Hotkeys;
using NotifyIcon;

namespace DeadEye.Windows;

public partial class DummyWindow : IDisposable
{
	private AboutWindow _aboutWindow;
	private ColorBrowserWindow _colorBrowserWindow;
	private Hotkey _overlayHotkey;
	private ScreenshotFrameWindow _screenshotWindow;
	private SettingsWindow _settingsWindow;

	#region Hotkey Actions

	private void OverlayHotkeyAction(Hotkey key)
	{
		Debug.WriteLine("Overlay Hotkey!");

		if (this._screenshotWindow != null)
		{
			Debug.WriteLine("Window is already open");
			return;
		}

		using var bm = ScreenshotHelper.GetFullscreenScreenshotGDI();
		var bitmapImage = bm.ToBitmapSource();

		this._screenshotWindow = new ScreenshotFrameWindow(bitmapImage);
		this._screenshotWindow.ScreenshotTaken += (sender, args) =>
		{
			Debug.WriteLine("Screenshot taken.");
			var croppedBitmap = new CroppedBitmap(bitmapImage, args.CroppedRect);

			try
			{
				Clipboard.SetImage(croppedBitmap);
			}
			catch (COMException e)
			{
				var message = string.Join("\n", new string[]
				{
					"Can't copy image into the Clipboard.",
					"To fix this, click here to open the Settings and click \"Clear clipboard data\".",
					$"More Info: 0x{e.HResult:X8}",
				});
				this.TaskbarIcon.ShowBalloonTip("Clipboard Error", message, BalloonIcon.Error);
			}

			bitmapImage = null;
		};
		this._screenshotWindow.Closed += (sender, args) => { this._screenshotWindow = null; };

		this._screenshotWindow.Show();
	}

	#endregion

	#region Initialization and Shutdown

	[DllImport("user32.dll")]
	private static extern nint SetParent(nint hwnd, nint hwndNewParent);

	private void DummyWindow_OnSourceInitialized(object sender, EventArgs e)
	{
		Debug.WriteLine("OnSourceInitialized");

		// Make screenshotWindow message-only
		const int HWND_MESSAGE = -3;
		if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
			SetParent(hwndSource.Handle, HWND_MESSAGE);

		// Register hotkeys
		this._overlayHotkey = new Hotkey(ModifierKeys.Alt | ModifierKeys.Shift, Key.S, this, this.OverlayHotkeyAction);
		
		this.TaskbarIcon.TrayMiddleMouseUp += (_, _) =>
		{
			Process.Start("ms-settings:clipboard");
		};
	}

	public void Dispose()
	{
		this._overlayHotkey.Dispose();
	}

	#endregion

	#region Context Menu Actions

	private void ColorMenuItem_OnClick(object sender, RoutedEventArgs e)
	{
		if (this._colorBrowserWindow != null)
		{
			this._colorBrowserWindow.Activate();
			return;
		}

		this._colorBrowserWindow = new ColorBrowserWindow();
		this._colorBrowserWindow.Closed += (a, b) => this._colorBrowserWindow = null;
		this._colorBrowserWindow.Show();
	}

	private void AppDirMenuItem_OnClick(object sender, RoutedEventArgs e)
	{
		var appPath = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
		var psi = new ProcessStartInfo("explorer.exe", $"/select,\"{appPath}\"");
		Debug.WriteLine($"{psi.FileName} {psi.Arguments}");

		Process.Start(psi);
	}

	private void GarbageMenuItem_OnClick(object sender, RoutedEventArgs e)
	{
		GCHelper.CleanUp();
	}

	private void SettingsMenuItem_OnClick(object sender, RoutedEventArgs e)
	{
		if (this._settingsWindow != null)
		{
			this._settingsWindow.Activate();
			return;
		}

		this._settingsWindow = new SettingsWindow();
		this._settingsWindow.Closed += (a, b) => this._settingsWindow = null;
		this._settingsWindow.Show();
	}

	private void AboutMenuItem_OnClick(object sender, RoutedEventArgs e)
	{
		if (this._aboutWindow != null)
		{
			this._aboutWindow.Activate();
			return;
		}

		this._aboutWindow = new AboutWindow();
		this._aboutWindow.Closed += (a, b) => this._aboutWindow = null;
		this._aboutWindow.Show();
	}

	private void ExitMenuItem_OnClick(object sender, RoutedEventArgs e)
	{
		Settings.SharedSettings.Save();
		Application.Current.Shutdown();
	}

	#endregion
}
