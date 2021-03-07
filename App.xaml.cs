using System.Windows;
using DeadEye.Helpers;

namespace DeadEye {
	public partial class App {
		private void App_OnStartup(object sender, StartupEventArgs e) {
			if (SingleInstanceHelper.IsOtherInstanceRunning()) {
				MessageBox.Show("There's already a running instance of DeadEye.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				this.Shutdown();
			}

			SingleInstanceHelper.Release();
		}

		public bool IsDebugMode {
			get {
#if DEBUG
				return true;
#else
				return false;
#endif
			}
		}
	}
}