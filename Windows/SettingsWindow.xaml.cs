using System.ComponentModel;

namespace DeadEye.Windows {
	public partial class SettingsWindow {

		public static GridType[] GridTypes => new[]
				{
					GridType.None,
					GridType.RuleOfThirds,
				};

		public SettingsWindow() {
			this.InitializeComponent();
		}

		private void SettingsWindow_OnClosing(object sender, CancelEventArgs e) {
			// Instead of saving after every little change, save when the settings window closes
			Settings.SharedSettings.Save();
		}

	}
}
