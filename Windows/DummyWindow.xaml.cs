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
using DeadEye.Win32;
using NotifyIcon;

namespace DeadEye.Windows;

public partial class DummyWindow
{
	private AboutWindow? _aboutWindow;
	private ScreenshotFrameWindow? _screenshotWindow;
	private SettingsWindow? _settingsWindow;

	#region Initialization and Shutdown

	private void DummyWindow_OnSourceInitialized(object sender, EventArgs e)
	{
		Debug.WriteLine("OnSourceInitialized");

		// Make screenshotWindow message-only
		const int HWND_MESSAGE = -3;
		if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
			User32.SetParent(hwndSource.Handle, HWND_MESSAGE);

		// initialize the hotkey manager and give it a reference to this window because *some* window is needed to receive events
		HotkeyManager.InitShared(this);
		HotkeyManager.Shared.RegisterHotkey();
		HotkeyManager.Shared.HotkeyPressed += this.OverlayHotkeyAction;

		this.TaskbarIcon.TrayMouseDoubleClick += (_, _) =>
		{
			var alt = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
			if (!alt)
				return;

			var psi = new ProcessStartInfo("ms-settings:clipboard") { UseShellExecute = true };
			Process.Start(psi);
		};
	}

	#endregion

	#region Hotkey Actions

	private void OverlayHotkeyAction(object _, HotkeyPressedEventArgs e)
	{
		if (Settings.Shared.WaitingForHotkey)
			return;

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
			catch (COMException ex)
			{
				var message = string.Join("\n", "Can't copy image into the Clipboard.", "To fix this, click here to open the Settings and click \"Clear clipboard data\".", $"More Info: 0x{ex.HResult:X8}");
				this.TaskbarIcon.ShowBalloonTip("Clipboard Error", message, BalloonIcon.Error);
			}

			bitmapImage = null;
		};
		this._screenshotWindow.Closed += (sender, args) => { this._screenshotWindow = null; };

		this._screenshotWindow.Show();
	}

	#endregion

	#region Context Menu Actions

	private void AppDirMenuItem_OnClick(object sender, RoutedEventArgs e)
	{
		var appPath = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
		var psi = new ProcessStartInfo("explorer.exe", $"/select,\"{appPath}\"");
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
		this._settingsWindow.Closed += (_, _) => this._settingsWindow = null;
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
		this._aboutWindow.Closed += (_, _) => this._aboutWindow = null;
		this._aboutWindow.Show();
	}

	private void ExitMenuItem_OnClick(object sender, RoutedEventArgs e)
	{
		Settings.Shared.Save();
		Application.Current.Shutdown();
	}

	#endregion
}
