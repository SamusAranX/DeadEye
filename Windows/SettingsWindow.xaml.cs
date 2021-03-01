using System.ComponentModel;
using System.Windows;

namespace DeadEye.Windows {
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow {

		public GridType[] GridTypes {
			get {
				return new[]
				{
					GridType.None,
					GridType.RuleOfThirds,
				};
			}
		}

		public SettingsWindow() {
			this.InitializeComponent();
		}

		private void SettingsWindow_OnClosing(object sender, CancelEventArgs e) {
			// Instead of saving after every little change, save when the settings window closes
			Settings.SharedSettings.Save();
		}
	}
}
