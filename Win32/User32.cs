using System.Runtime.InteropServices;
using System.Windows.Input;

namespace DeadEye.Win32;

internal sealed class User32
{
	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool RegisterHotKey(nint hWnd, int id, ModifierKeys fsModifiers, int vk);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool UnregisterHotKey(nint hWnd, int id);

	[DllImport("user32.dll")]
	public static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	public static extern nint SetParent(nint hwnd, nint hwndNewParent);
}
