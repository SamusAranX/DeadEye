using System.ComponentModel;

namespace DeadEye.Windows {
	public partial class SettingsWindow {
		public SettingsWindow() {
			this.InitializeComponent();
		}

		public static GridType[] GridTypes => new[] {
			GridType.None,
			GridType.RuleOfThirds
		};

		private void SettingsWindow_OnClosing(object sender, CancelEventArgs e) {
			// Instead of saving after every little change, save when the settings window closes
			Settings.SharedSettings.Save();
		}
	}
}