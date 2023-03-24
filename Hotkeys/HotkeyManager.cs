using System.Windows;
using System.Windows.Input;

namespace DeadEye.Hotkeys;

public sealed class HotkeyPressedEventArgs : EventArgs
{
	public readonly Key Key;
	public readonly ModifierKeys Modifiers;

	public HotkeyPressedEventArgs(ModifierKeys modifiers, Key key)
	{
		this.Modifiers = modifiers;
		this.Key = key;
	}
}

public delegate void HotkeyPressedEventHandler(object sender, HotkeyPressedEventArgs args);

internal sealed class HotkeyManager: IDisposable
{
	public event HotkeyPressedEventHandler? HotkeyPressed;

	private readonly Window _hotkeyWindow;
	private Hotkey? _overlayHotkey;

	public bool IsHotkeyRegistered => this._overlayHotkey != null;

	public HotkeyManager(Window window)
	{
		this._hotkeyWindow = window;
	}

	private void HotkeyPressedAction(Hotkey key)
	{
		var e = new HotkeyPressedEventArgs(key.KeyModifier, key.Key);
		this.HotkeyPressed?.Invoke(this, e);
	}

	public void RegisterHotkey()
	{
		if (this._overlayHotkey != null)
			return;

		this._overlayHotkey = new Hotkey(Settings.Shared.ScreenshotModifierKeys, Settings.Shared.ScreenshotKey, this._hotkeyWindow, this.HotkeyPressedAction);
	}

	public void UnregisterHotkey()
	{
		this._overlayHotkey?.Dispose();
		this._overlayHotkey = null;
	}

	public void Dispose()
	{
		this.UnregisterHotkey();
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
