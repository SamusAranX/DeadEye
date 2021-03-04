using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;

namespace DeadEye.Helpers {
	internal class AutostartHelper {
		private const string REGISTRY_KEY = "DeadEye";
		private const string RUN_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

		public static bool CheckAutostartStatus() {
			using var registryKey = Registry.CurrentUser.OpenSubKey(RUN_KEY, false);
			if (registryKey == null)
				throw new KeyNotFoundException("The Run key does not exist.");

			return registryKey.GetValue(REGISTRY_KEY) != null;
		}

		public static void EnableAutostart() {
			using var registryKey = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);
			if (registryKey == null)
				throw new KeyNotFoundException("The Run key does not exist.");

			registryKey.SetValue(REGISTRY_KEY, Assembly.GetExecutingAssembly().Location);
			Debug.WriteLine("Autostart enabled.");
		}

		public static void DisableAutostart() {
			using var registryKey = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);
			if (registryKey == null)
				throw new KeyNotFoundException("The Run key does not exist.");

			registryKey.DeleteValue(REGISTRY_KEY);
			Debug.WriteLine("Autostart disabled.");
		}
	}
}
