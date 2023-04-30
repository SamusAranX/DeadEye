using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using DeadEye.Win32;
using Microsoft.Win32;
using PInvoke;

#pragma warning disable CA1852

namespace DeadEye.Helpers;

public static class RECTExtensions
{
	public static Rect ToRect(this RECT rect)
	{
		return new Rect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
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

		var dpiX = Gdi32.GetDeviceCaps(safeScreenDC, Gdi32.DeviceCap.LOGPIXELSX);
		var dpiY = Gdi32.GetDeviceCaps(safeScreenDC, Gdi32.DeviceCap.LOGPIXELSY);
		if (dpiX != dpiY)
			throw new ArgumentOutOfRangeException($"DPI mismatch: {dpiX} != {dpiY}");

		this.DotsPerInch = dpiX;

		if (hdc != screenDC)
			Gdi32.DeleteDC(safeScreenDC);
	}

	private static Point TopLeftCorner { get; set; }

	/// <summary>
	/// The display device's name. Typically follows the pattern "\\.\DISPLAY1".
	/// </summary>
	public string DeviceName { get; }

	/// <summary>
	/// The product of bits per pixel and number of channels. Typically 32 for normal displays, might be more for deep color or HDR displays.
	/// </summary>
	public int BitDepth { get; }

	/// <summary>
	/// The number of pixels per inch.
	/// </summary>
	public int DotsPerInch { get; }

	/// <summary>
	/// Whether this is the primary display.
	/// </summary>
	public bool IsPrimary { get; }

	/// <summary>
	/// The rectangle this screen occupies on the larger virtual screen rect. Location might have negative components.
	/// </summary>
	public Rect Bounds { get; }

	/// <summary>
	/// The location of the Bounds property.
	/// </summary>
	public Point Location => this.Bounds.Location;

	/// <summary>
	/// The location of the Bounds property, but normalized to have positive X and Y components.
	/// </summary>
	public Point LocationNormalized => new(this.Location.X - TopLeftCorner.X, this.Location.Y - TopLeftCorner.Y);

	/// <summary>
	/// This screen's resolution in pixels.
	/// </summary>
	public Size Resolution => new(this.Bounds.Right - this.Bounds.Left, this.Bounds.Bottom - this.Bounds.Top);

	/// <summary>
	/// The rectangle this screen occupies on the larger virtual screen rect, but normalized to have positive X and Y components.
	/// </summary>
	public Rect BoundsNormalized => new(this.LocationNormalized, this.Resolution);

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

				var leftmostScreenX = _screens.OrderBy(s => s.Location.X).First().Location.X;
				var topmostScreenY = _screens.OrderBy(s => s.Location.Y).First().Location.Y;
				TopLeftCorner = new Point(leftmostScreenX, topmostScreenY);
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
	/// changing. We cache screen information and at this point we must
	/// invalidate our cache.
	/// </summary>
	private static void OnDisplaySettingsChanging(object? sender, EventArgs e)
	{
		Debug.WriteLine("OnDisplaySettingsChanging");

		// Now that we've responded to this event, we don't need it again until
		// someone re-queries. We will re-add the event at that time.
		SystemEvents.DisplaySettingsChanging -= OnDisplaySettingsChanging;

		// Display settings changed, so the set of screens we have is invalid.
		_screens = null;
	}

	/// <summary>
	/// Retrieves a <see cref="Screen" /> for the monitor that contains the specified point.
	/// </summary>
	public static Screen FromPoint(Point point)
	{
		var pt = default(POINT);
		pt.x = (int)point.X;
		pt.y = (int)point.Y;

		return SystemInformation.MultiMonitorSupport
			? new Screen(User32.MonitorFromPoint(pt, User32.MonitorOptions.MONITOR_DEFAULTTONEAREST))
			: new Screen(PRIMARY_MONITOR);
	}

	/// <summary>
	/// Retrieves a <see cref="Screen" /> for the monitor that contains the specified point, but correcting for normalized coordinates.
	/// </summary>
	public static Screen FromNormalizedPoint(Point point)
	{
		var pt = default(POINT);
		pt.x = (int)(point.X + TopLeftCorner.X);
		pt.y = (int)(point.Y + TopLeftCorner.Y);

		return SystemInformation.MultiMonitorSupport
			? new Screen(User32.MonitorFromPoint(pt, User32.MonitorOptions.MONITOR_DEFAULTTONEAREST))
			: new Screen(PRIMARY_MONITOR);
	}

	/// <summary>
	/// Retrieves a string representing this object.
	/// </summary>
	public override string ToString()
	{
		return $"{this.GetType().Name}[DeviceName={this.DeviceName} Primary={this.IsPrimary} BitDepth={this.BitDepth} DPI={this.DotsPerInch} Bounds={this.Bounds}]";
	}
}
