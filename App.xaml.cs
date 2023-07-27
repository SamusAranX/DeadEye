using System.Diagnostics;
using System.Windows;
using DeadEye.Helpers;
using DeadEye.Hotkeys;

namespace DeadEye;

public partial class App : IDisposable
{
	private SingleInstanceHelper? _singleInstanceHelper;

	public void Dispose()
	{
		this._singleInstanceHelper?.Dispose();
		HotkeyManager.Shared.Dispose();
	}

	private void App_OnStartup(object sender, StartupEventArgs e)
	{
		this._singleInstanceHelper = new SingleInstanceHelper("DeadEye");

		if (this._singleInstanceHelper.IsOtherInstanceRunning())
		{
			MessageBox.Show("There's already a running instance of DeadEye.", "Instance already running", MessageBoxButton.OK, MessageBoxImage.Warning);
			this.Shutdown();
		}

		for (var i = 0; i < Screen.AllScreens.Length; i++)
		{
			var screen = Screen.AllScreens[i];
			var isPrimary = screen.IsPrimary ? "*" : "";
			Debug.WriteLine($"[{i}{isPrimary}] {screen}");
		}
	}
}
