using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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

	[Description("Debugging")]
	Debugging,
}

internal sealed class AutostartHelper
{
	private const string KEY_NAME = "DeadEye";
	private const string RUN_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
	private const string RUN_APPROVED_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

	private static readonly string APP_PATH = $"\"{Path.Join(AppContext.BaseDirectory, Path.GetFileName(Environment.GetCommandLineArgs()[0]))}\"";
	private static readonly bool APP_IS_DEBUG = !APP_PATH.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase);

	public static bool CheckAutostartStatus()
	{
		if (APP_IS_DEBUG)
		{
			Debug.WriteLine("App is not an .exe, skipping autostart check.");
			return false;
		}

		using var registryKey = Registry.CurrentUser.OpenSubKey(RUN_KEY, false);
		if (registryKey == null)
			throw new KeyNotFoundException("The Run key does not exist.");

		var value = registryKey.GetValue(KEY_NAME);
		if (value == null)
			return false;

		if ((string)value != APP_PATH)
		{
			Debug.WriteLine($"Registry contains outdated path {(string)value}.");
			Debug.WriteLine($"Updating registry using new path {APP_PATH}.");
			EnableAutostart(); // refresh binary path in the registry
		}

		return true;
	}

	/// <summary>Checks the autostart status of the app.</summary>
	/// <returns>The current autostart status.</returns>
	public static AutostartStatus GetTaskmgrAutostartStatus()
	{
		if (APP_IS_DEBUG)
			return AutostartStatus.Debugging;

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
				var dateTime = DateTime.FromFileTimeUtc(dateLong);
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
		if (APP_IS_DEBUG)
		{
			Trace.WriteLine("CALLED EnableAutostart() EVEN THOUGH APP IS NOT RUNNING AS .EXE");
			return;
		}

		using var registryKey = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);
		if (registryKey == null)
			throw new KeyNotFoundException("The Run key does not exist.");

		registryKey.SetValue(KEY_NAME, APP_PATH);
		Debug.WriteLine("Autostart enabled.");
	}

	public static void DisableAutostart()
	{
		if (APP_IS_DEBUG)
		{
			Trace.WriteLine("CALLED DisableAutostart() EVEN THOUGH APP IS NOT RUNNING AS .EXE");
			return;
		}

		using var registryKey = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);
		if (registryKey == null)
			throw new KeyNotFoundException("The Run key does not exist.");

		registryKey.DeleteValue(KEY_NAME);
		Debug.WriteLine("Autostart disabled.");
	}
}
