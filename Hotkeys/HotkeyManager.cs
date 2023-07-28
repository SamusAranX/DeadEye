using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace DeadEye.Hotkeys;

internal sealed class HotkeyPressedEventArgs : EventArgs
{
	public readonly Key Key;
	public readonly ModifierKeys Modifiers;
	public readonly HotkeyType Type;

	public HotkeyPressedEventArgs(ModifierKeys modifiers, Key key, HotkeyType type)
	{
		this.Modifiers = modifiers;
		this.Key = key;
		this.Type = type;
	}
}

internal delegate void HotkeyPressedEventHandler(object sender, HotkeyPressedEventArgs args);

public enum HotkeyType
{
	Screenshot,
	ColorPicker,
}

internal sealed class HotkeyManager : IDisposable
{
	private readonly Window _hotkeyWindow;

	private Hotkey? _screenshotHotkey;
	private Hotkey? _colorPickerHotkey;

	public HotkeyManager(Window window)
	{
		this._hotkeyWindow = window;
	}

	public bool IsScreenshotHotkeyRegistered => this._screenshotHotkey != null;
	public bool IsColorPickerHotkeyRegistered => this._colorPickerHotkey != null;

	public void Dispose()
	{
		this.UnregisterHotkeys();
	}

	public event HotkeyPressedEventHandler? HotkeyPressed;

	private void HotkeyPressedAction(Hotkey key)
	{
		HotkeyType type;
		if (Settings.Shared.ScreenshotKey != null && key.KeyModifier == Settings.Shared.ScreenshotKey.ModifierKeys && key.Key == Settings.Shared.ScreenshotKey.Key)
			type = HotkeyType.Screenshot;
		else if (Settings.Shared.ColorPickerKey != null && key.KeyModifier == Settings.Shared.ColorPickerKey.ModifierKeys && key.Key == Settings.Shared.ColorPickerKey.Key)
			type = HotkeyType.ColorPicker;
		else
		{
			Debug.WriteLine("Invalid Hotkey configuration");
			return;
		}

		var e = new HotkeyPressedEventArgs(key.KeyModifier, key.Key, type);
		this.HotkeyPressed?.Invoke(this, e);
	}

	public void RegisterHotkeys()
	{
		if (this._screenshotHotkey == null && Settings.Shared.ScreenshotKey != null)
		{
			this._screenshotHotkey = new Hotkey(Settings.Shared.ScreenshotKey.ModifierKeys, Settings.Shared.ScreenshotKey.Key, this._hotkeyWindow, this.HotkeyPressedAction);
			Debug.WriteLine("Screenshot Hotkey registered");
		}

		if (this._colorPickerHotkey == null && Settings.Shared.ColorPickerKey != null)
		{
			this._colorPickerHotkey = new Hotkey(Settings.Shared.ColorPickerKey.ModifierKeys, Settings.Shared.ColorPickerKey.Key, this._hotkeyWindow, this.HotkeyPressedAction);
			Debug.WriteLine("Color Picker Hotkey registered");
		}
	}

	public void UnregisterHotkeys()
	{
		this._screenshotHotkey?.Dispose();
		this._screenshotHotkey = null;
		this._colorPickerHotkey?.Dispose();
		this._colorPickerHotkey = null;
	}

	#region Singleton

	private static HotkeyManager? _sharedManager;

	public static HotkeyManager Shared
	{
		get => _sharedManager!;
		set => _sharedManager = value;
	}

	public static void InitShared(Window window)
	{
		_sharedManager = new HotkeyManager(window);
	}

	#endregion
}
