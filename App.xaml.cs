using System.Windows;
using DeadEye.Helpers;

namespace DeadEye;

public partial class App : IDisposable
{
	private SingleInstanceHelper _singleInstanceHelper;

	public bool IsDebugMode
	{
		get
		{
#if DEBUG
			return true;
#else
				return false;
#endif
		}
	}

	private void App_OnStartup(object sender, StartupEventArgs e)
	{
		this._singleInstanceHelper = new SingleInstanceHelper("DeadEye");

		if (this._singleInstanceHelper.IsOtherInstanceRunning())
		{
			MessageBox.Show("There's already a running instance of DeadEye.", "Error starting DeadEye", MessageBoxButton.OK, MessageBoxImage.Warning);
			this.Shutdown();
		}
	}

	public void Dispose()
	{
		this._singleInstanceHelper.Dispose();
	}
}
