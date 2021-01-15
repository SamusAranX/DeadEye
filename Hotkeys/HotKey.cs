using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DeadEye.Hotkeys {
    public sealed class HotKey : IDisposable {
	    private const int WM_HOTKEY = 0x0312;
	    private const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

	    [DllImport("user32.dll", SetLastError = true)]
	    private static extern bool RegisterHotKey(IntPtr hWnd, int id, ModifierKeys fsModifiers, int vk);

	    [DllImport("user32.dll", SetLastError = true)]
	    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private readonly IntPtr _handle;

        private readonly int _id;

        private bool _isKeyRegistered;

        private readonly Dispatcher _currentDispatcher;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public HotKey(ModifierKeys modifierKeys, Key key, Window window)
            : this(modifierKeys, key, new WindowInteropHelper(window), null) {
        }

        public HotKey(ModifierKeys modifierKeys, Key key, WindowInteropHelper window)
            : this(modifierKeys, key, window.Handle, null) {
        }

        public HotKey(ModifierKeys modifierKeys, Key key, Window window, Action<HotKey> onKeyAction)
            : this(modifierKeys, key, new WindowInteropHelper(window), onKeyAction) {
        }

        public HotKey(ModifierKeys modifierKeys, Key key, WindowInteropHelper window, Action<HotKey> onKeyAction)
            : this(modifierKeys, key, window.Handle, onKeyAction) {
        }

        public HotKey(ModifierKeys modifierKeys, Key key, IntPtr windowHandle, Action<HotKey> onKeyAction = null) {
            this.Key = key;
            this.KeyModifier = modifierKeys;
            this._id = this.GetHashCode();
            this._handle = windowHandle == IntPtr.Zero ? GetForegroundWindow() : windowHandle;
            this._currentDispatcher = Dispatcher.CurrentDispatcher;
            this.RegisterHotKey();
            ComponentDispatcher.ThreadPreprocessMessage += this.ThreadPreprocessMessageMethod;

            if (onKeyAction != null)
	            this.HotKeyPressed += onKeyAction;
        }

        ~HotKey() {
	        this.Dispose();
        }

        public event Action<HotKey> HotKeyPressed;

        public Key Key { get; }

        public ModifierKeys KeyModifier { get; }

        private int InteropKey => KeyInterop.VirtualKeyFromKey(this.Key);

        public void Dispose() {
            try {
                ComponentDispatcher.ThreadPreprocessMessage -= this.ThreadPreprocessMessageMethod;
            } catch (Exception) {
                // ignored
            } finally {
                this.UnregisterHotKey();
            }
        }

        private void OnHotKeyPressed() {
	        this._currentDispatcher.Invoke(
                delegate {
	                this.HotKeyPressed?.Invoke(this);
                });
        }

        private void RegisterHotKey() {
            if (this.Key == Key.None) {
                return;
            }
            
            if (this.KeyModifier.HasFlag(ModifierKeys.Windows)) {
				Debug.WriteLine("The Windows key cannot be used in hotkeys.");
	            return;
            }

            if (this._isKeyRegistered) {
	            this.UnregisterHotKey();
            }

            this._isKeyRegistered = RegisterHotKey(this._handle, this._id, this.KeyModifier, this.InteropKey);

            if (!this._isKeyRegistered) {
	            var errorCode = Marshal.GetLastWin32Error();

	            if (errorCode == ERROR_HOTKEY_ALREADY_REGISTERED) {
					Debug.WriteLine("Hotkey already registered.");
	            } else {
		            throw new ApplicationException($"Couldn't register hotkey. Error code {errorCode}");
                }
            }
        }

        private void ThreadPreprocessMessageMethod(ref MSG msg, ref bool handled) {
            if (handled) {
                return;
            }

            if (msg.message != WM_HOTKEY || (int)(msg.wParam) != this._id) {
                return;
            }

            this.OnHotKeyPressed();
            handled = true;
        }

        private void UnregisterHotKey() {
	        this._isKeyRegistered = !UnregisterHotKey(this._handle, this._id);
        }
    }
}
