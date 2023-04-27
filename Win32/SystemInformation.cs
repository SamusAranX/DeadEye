using System.Windows;
using PInvoke;

namespace DeadEye.Win32;

internal sealed class SystemInformation
{
	private static bool? _multiMonitorSupport;

	internal static bool MultiMonitorSupport
	{
		get
		{
			_multiMonitorSupport ??= User32.GetSystemMetrics(User32.SystemMetric.SM_CMONITORS) != 0;
			return _multiMonitorSupport.Value;
		}
	}

	/// <summary>
	/// Gets the number of display monitors on the desktop.
	/// </summary>
	public static int MonitorCount => MultiMonitorSupport ? User32.GetSystemMetrics(User32.SystemMetric.SM_CMONITORS) : 1;

	/// <summary>
	/// Gets the dimensions of the primary display monitor in pixels.
	/// </summary>
	public static Size PrimaryMonitorSize => GetSize(User32.SystemMetric.SM_CXSCREEN, User32.SystemMetric.SM_CYSCREEN);

	/// <summary>
	/// Gets the bounds of the virtual screen.
	/// </summary>
	public static Rect VirtualScreen
	{
		get
		{
			if (MultiMonitorSupport)
			{
				return new Rect(User32.GetSystemMetrics(User32.SystemMetric.SM_XVIRTUALSCREEN),
					User32.GetSystemMetrics(User32.SystemMetric.SM_YVIRTUALSCREEN),
					User32.GetSystemMetrics(User32.SystemMetric.SM_CXVIRTUALSCREEN),
					User32.GetSystemMetrics(User32.SystemMetric.SM_CYVIRTUALSCREEN));
			}

			var size = PrimaryMonitorSize;
			return new Rect(0, 0, size.Width, size.Height);
		}
	}

	private static Size GetSize(User32.SystemMetric x, User32.SystemMetric y)
	{
		return new Size(User32.GetSystemMetrics(x), User32.GetSystemMetrics(y));
	}
}
