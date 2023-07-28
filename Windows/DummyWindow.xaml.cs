using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DeadEye.Helpers;
using DeadEye.Hotkeys;
using NotifyIcon;
using PInvoke;

namespace DeadEye.Windows;

public partial class DummyWindow
{
	private AboutWindow? _aboutWindow;
	private ScreenshotFrameWindow? _screenshotWindow;
	private ColorPickerWindow? _colorPickerWindow;
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
		HotkeyManager.Shared.RegisterHotkeys();
		HotkeyManager.Shared.HotkeyPressed += this.HotkeyPressedHandler;

		this.TaskbarIcon.TrayBalloonTipClicked += (_, _) =>
		{
			var psi = new ProcessStartInfo("ms-settings:clipboard") { UseShellExecute = true };
			Process.Start(psi);
		};

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

	#region Clipboard methods

	private void ClipboardSetText(string text)
	{
		try
		{
			Clipboard.SetText(text);
		}
		catch (COMException ex)
		{
			var message = string.Join("\n", "Can't copy text into the Clipboard.", "To fix this, click here to open the Settings and click \"Clear clipboard data\".", $"More Info: 0x{ex.HResult:X8}");
			this.TaskbarIcon.ShowBalloonTip("Clipboard Error", message, BalloonIcon.Error);
		}
	}

	private void ClipboardSetImage(BitmapSource image)
	{
		try
		{
			Clipboard.SetImage(image);
		}
		catch (COMException ex)
		{
			var message = string.Join("\n", "Can't copy image into the Clipboard.", "To fix this, click here to open the Settings and click \"Clear clipboard data\".", $"More Info: 0x{ex.HResult:X8}");
			this.TaskbarIcon.ShowBalloonTip("Clipboard Error", message, BalloonIcon.Error);
		}
	}

	#endregion

	#region Hotkey Handlers

	private void ScreenshotTaken(object sender, ScreenshotEventArgs args)
	{
		Debug.WriteLine("Screenshot taken.");
		this.ClipboardSetImage(args.Screenshot);
	}

	private void ColorPicked(object sender, ColorPickEventArgs args)
	{
		Debug.WriteLine("Color picked.");
		var c = args.PickedColor;
		this.ClipboardSetText($"#{c.R:X2}{c.G:X2}{c.B:X2}");
	}

	private void HotkeyPressedHandler(object _, HotkeyPressedEventArgs e)
	{
		if (Settings.Shared.WaitingForHotkey.HasValue)
			return;

		Debug.WriteLine("Hotkey Pressed!");

		if (this._screenshotWindow != null || this._colorPickerWindow != null)
		{
			Debug.WriteLine("A Window is already open");
			return;
		}

		// get screenshot bitmap and convert to bitmapsource
		using var bm = ScreenshotHelper.GetFullscreenScreenshotGDI();
		var hBitmap = bm.GetHbitmap();
		var bitmapImage = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
		bitmapImage.Freeze();

		if (!Gdi32.DeleteObject(hBitmap))
			Debug.WriteLine("Couldn't delete screenshot bitmap");

		switch (e.Type)
		{
			case HotkeyType.Screenshot:
				this._screenshotWindow = new ScreenshotFrameWindow(ref bitmapImage);
				this._screenshotWindow.Closed += (_, _) => { this._screenshotWindow = null; };
				this._screenshotWindow.ScreenshotTaken += this.ScreenshotTaken;
				this._screenshotWindow.Show();
				break;
			case HotkeyType.ColorPicker:

				this._colorPickerWindow = new ColorPickerWindow(ref bitmapImage);
				this._colorPickerWindow.Closed += (_, _) => { this._colorPickerWindow = null; };
				this._colorPickerWindow.ColorPicked += this.ColorPicked;
				this._colorPickerWindow.Show();
				break;
			default:
				throw new NotImplementedException($"Invalid hotkey type {e.Type}");
		}
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
