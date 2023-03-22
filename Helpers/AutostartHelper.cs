using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;

namespace DeadEye.Helpers;

public enum AutostartStatus
{
	[Description("Unknown")]
	Unknown,

	[Description("Disabled in the Task Manager")]
	Disabled,

	[Description("Enabled")]
	Enabled,
}

internal sealed class AutostartHelper
{
#if DEBUG
	private const string KEY_NAME = "DeadEye (Debug)";
#else
	private const string KEY_NAME = "DeadEye";
#endif

	private const string RUN_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
	private const string RUN_APPROVED_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

	private static readonly string APP_PATH = $"\"{Assembly.GetExecutingAssembly().Location}\"";

	public static bool CheckAutostartStatus()
	{
		using var registryKey = Registry.CurrentUser.OpenSubKey(RUN_KEY, false);
		if (registryKey == null)
			throw new KeyNotFoundException("The Run key does not exist.");

		var value = registryKey.GetValue(KEY_NAME);
		if (value == null)
			return false;

		if ((string)value != APP_PATH)
		{
			Debug.WriteLine("Registry contains outdated path. Updating.");
			EnableAutostart(); // refresh binary path in the registry
		}

		return true;
	}

	/// <summary>
	/// Checks the task manager autostart status of the app.
	/// </summary>
	/// <returns>
	/// A Tuple of AutostartStatus and (if explicitly disabled) a DateTime object representing the date and time at
	/// which autostart was disabled in the Task Manager.
	/// </returns>
	public static AutostartStatus GetTaskmgrAutostartStatus()
	{
		using var registryKey = Registry.CurrentUser.OpenSubKey(RUN_APPROVED_KEY, false);

		var value = registryKey?.GetValue(KEY_NAME);
		if (value == null)
			return AutostartStatus.Unknown;

		try
		{
			var byteArray = (byte[])value;
			var headerValue = BitConverter.ToInt32(byteArray, 0);
			Debug.WriteLine($"key header: {headerValue}");

			if (headerValue == 2) // 2: enabled
				return AutostartStatus.Enabled;

			if (headerValue == 3)
			{ 
				// 3: disabled
				var dateLong = BitConverter.ToInt64(byteArray, 4);
				var dateTime = DateTime.FromFileTime(dateLong);
				return AutostartStatus.Disabled;
			}

			Debug.WriteLine($"Unknown header value {headerValue}");
		}
		catch (Exception e)
		{
			Debug.WriteLine(e);
		}

		return AutostartStatus.Unknown;
	}

	public static void EnableAutostart()
	{
		using var registryKey = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);
		if (registryKey == null)
			throw new KeyNotFoundException("The Run key does not exist.");

		registryKey.SetValue(KEY_NAME, APP_PATH);
		Debug.WriteLine("Autostart enabled.");
	}

	public static void DisableAutostart()
	{
		using var registryKey = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);
		if (registryKey == null)
			throw new KeyNotFoundException("The Run key does not exist.");

		registryKey.DeleteValue(KEY_NAME);
		Debug.WriteLine("Autostart disabled.");
	}
}
