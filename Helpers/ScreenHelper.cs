using System.Runtime.InteropServices;
using System.Windows;
using DeadEye.Win32;
using Microsoft.Win32;
using PInvoke;

namespace DeadEye.Helpers;

public static class RECTExtensions
{
	public static Rect ToRect(this RECT rect)
	{
		return new Rect(rect.left, rect.top, rect.right, rect.bottom);
	}
}

internal class MonitorEnumCallback
{
	public List<Screen> Screens = new();

	public virtual unsafe bool Callback(nint monitor, nint hdc, RECT* lprcMonitor, void* lparam)
	{
		this.Screens.Add(new Screen(monitor, hdc));
		return true;
	}
}

internal sealed class Screen
{
	private const int PRIMARY_MONITOR = unchecked((int)0xBAADF00D);

	private static Screen[]? _screens;

	internal Screen(IntPtr monitor) : this(monitor, default) { }

	internal unsafe Screen(IntPtr monitor, IntPtr hdc)
	{
		var screenDC = hdc;

		if (!SystemInformation.MultiMonitorSupport || monitor == PRIMARY_MONITOR)
		{
			this.Bounds = SystemInformation.VirtualScreen;
			this.IsPrimary = true;
			this.DeviceName = "DISPLAY";
		}
		else
		{
			var info = new User32.MONITORINFOEX
			{
				cbSize = sizeof(User32.MONITORINFOEX),
			};
			User32.GetMonitorInfo(monitor, &info);
			this.Bounds = info.Monitor.ToRect();
			this.IsPrimary = (info.Flags & User32.MONITORINFO_Flags.MONITORINFOF_PRIMARY) != 0;
			this.DeviceName = Marshal.PtrToStringUni((nint)info.DeviceName) ?? "";

			if (hdc == IntPtr.Zero)
				screenDC = CreateDCW(this.DeviceName, "", null, IntPtr.Zero);
		}

		var safeScreenDC = new User32.SafeDCHandle(IntPtr.Zero, screenDC);
		this.BitDepth = Gdi32.GetDeviceCaps(safeScreenDC, Gdi32.DeviceCap.BITSPIXEL);
		this.BitDepth *= Gdi32.GetDeviceCaps(safeScreenDC, Gdi32.DeviceCap.PLANES);

		if (hdc != screenDC)
			Gdi32.DeleteDC(safeScreenDC);
	}

	public bool IsPrimary { get; }
	public Rect Bounds { get; }
	public Point VirtualScreenPosition => this.Bounds.Location;
	public Size Resolution => this.Bounds.Size;
	public string DeviceName { get; }
	public int BitDepth { get; }

	/// <summary>
	/// Gets an array of all of the displays on the system.
	/// </summary>
	public static unsafe Screen[] AllScreens
	{
		get
		{
			if (_screens == null)
			{
				if (SystemInformation.MultiMonitorSupport)
				{
					var closure = new MonitorEnumCallback();
					var proc = new User32.MONITORENUMPROC(closure.Callback);
					User32.EnumDisplayMonitors(nint.Zero, null, proc, null);

					if (closure.Screens.Count > 0)
						_screens = closure.Screens.ToArray();
					else
						_screens = new[] { new Screen(PRIMARY_MONITOR) };
				}
				else
					_screens = new[] { PrimaryScreen! };
			}

			SystemEvents.DisplaySettingsChanging += OnDisplaySettingsChanging;

			return _screens;
		}
	}

	/// <summary>
	/// Gets the primary display.
	/// </summary>
	public static Screen? PrimaryScreen
	{
		get
		{
			if (!SystemInformation.MultiMonitorSupport)
				return new Screen(PRIMARY_MONITOR, default);

			var screens = AllScreens;
			foreach (var s in screens)
			{
				if (s.IsPrimary)
					return s;
			}

			return null;
		}
	}

	[DllImport("gdi32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
	public static extern IntPtr CreateDCW(string pwszDriver, string pwszDevice, string? pszPort, IntPtr pdm);

	/// <summary>
	/// Called by the SystemEvents class when our display settings are
	/// changing.  We cache screen information and at this point we must
	/// invalidate our cache.
	/// </summary>
	private static void OnDisplaySettingsChanging(object? sender, EventArgs e)
	{
		// Now that we've responded to this event, we don't need it again until
		// someone re-queries. We will re-add the event at that time.
		SystemEvents.DisplaySettingsChanging -= OnDisplaySettingsChanging;

		// Display settings changed, so the set of screens we have is invalid.
		_screens = null;
	}

	/// <summary>
	/// Retrieves a string representing this object.
	/// </summary>
	public override string ToString()
	{
		return $"{this.GetType().Name}[Bounds={this.Bounds} Primary={this.IsPrimary} DeviceName={this.DeviceName}]";
	}
}
