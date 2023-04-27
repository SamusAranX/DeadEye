using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using DeadEye.Win32;
using PInvoke;

namespace DeadEye.Hotkeys;

public sealed class HotkeyException : Exception
{
	public HotkeyException() { }

	public HotkeyException(string message) : base(message) { }

	public HotkeyException(string message, Exception inner) : base(message, inner) { }
}

public sealed class Hotkey : IDisposable
{
	private const int WM_HOTKEY = 0x0312;
	private const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

	private readonly Dispatcher _currentDispatcher;

	private readonly nint _handle;

	private readonly int _id;

	private bool _isKeyRegistered;

	public Hotkey(ModifierKeys modifierKeys, Key key, Window window)
		: this(modifierKeys, key, new WindowInteropHelper(window), null) { }

	public Hotkey(ModifierKeys modifierKeys, Key key, WindowInteropHelper window)
		: this(modifierKeys, key, window.Handle) { }

	public Hotkey(ModifierKeys modifierKeys, Key key, Window window, Action<Hotkey>? onKeyAction)
		: this(modifierKeys, key, new WindowInteropHelper(window), onKeyAction) { }

	public Hotkey(ModifierKeys modifierKeys, Key key, WindowInteropHelper window, Action<Hotkey>? onKeyAction)
		: this(modifierKeys, key, window.Handle, onKeyAction) { }

	public Hotkey(ModifierKeys modifierKeys, Key key, nint windowHandle, Action<Hotkey>? onKeyAction = null)
	{
		this.Key = key;
		this.KeyModifier = modifierKeys;
		this._id = this.GetHashCode();
		this._handle = windowHandle == nint.Zero ? User32.GetForegroundWindow() : windowHandle;
		this._currentDispatcher = Dispatcher.CurrentDispatcher;
		this.RegisterHotkey();
		ComponentDispatcher.ThreadPreprocessMessage += this.ThreadPreprocessMessageMethod;

		if (onKeyAction != null)
			this.HotkeyPressedEventHandler += onKeyAction;
	}

	public Key Key { get; }

	public ModifierKeys KeyModifier { get; }

	private int InteropKey => KeyInterop.VirtualKeyFromKey(this.Key);

	public void Dispose()
	{
		try
		{
			ComponentDispatcher.ThreadPreprocessMessage -= this.ThreadPreprocessMessageMethod;
		}
		catch (Exception)
		{
			// ignored
		}
		finally
		{
			this.UnregisterHotkey();
		}
	}

	~Hotkey()
	{
		this.Dispose();
	}

	public event Action<Hotkey>? HotkeyPressedEventHandler;

	private void OnHotkeyPressed()
	{
		this._currentDispatcher.Invoke(
			delegate { this.HotkeyPressedEventHandler?.Invoke(this); });
	}

	/// <summary>
	/// </summary>
	/// <exception cref="HotkeyException">An error has occurred that prevents the </exception>
	private void RegisterHotkey()
	{
		if (this.Key == Key.None)
			return;

		if (this.KeyModifier.HasFlag(ModifierKeys.Windows))
			throw new HotkeyException("The Windows key cannot be used in hotkeys.");

		if (this._isKeyRegistered)
			this.UnregisterHotkey();

		this._isKeyRegistered = HotKey.RegisterHotKey(this._handle, this._id, this.KeyModifier, this.InteropKey);

		if (!this._isKeyRegistered)
		{
			var errorCode = Marshal.GetLastWin32Error();

			if (errorCode == ERROR_HOTKEY_ALREADY_REGISTERED)
				Debug.WriteLine("Hotkey already registered.");
			else
				throw new HotkeyException($"Couldn't register hotkey. Error code {errorCode} (0x{errorCode:X8})");
		}
	}

	private void UnregisterHotkey()
	{
		this._isKeyRegistered = !HotKey.UnregisterHotKey(this._handle, this._id);
	}

	private void ThreadPreprocessMessageMethod(ref MSG msg, ref bool handled)
	{
		if (handled)
			return;

		if (msg.message != WM_HOTKEY || (int)msg.wParam != this._id)
			return;

		this.OnHotkeyPressed();
		handled = true;
	}
}
