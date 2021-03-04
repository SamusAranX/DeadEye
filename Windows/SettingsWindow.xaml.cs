using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using DeadEye.Helpers;

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

		private void ToggleButton_OnCheckUncheck(object sender, RoutedEventArgs e) {
			if (!this.IsLoaded)
				return;

			var checkbox = (CheckBox)sender;
			if (checkbox.IsChecked.HasValue && checkbox.IsChecked.Value)
				AutostartHelper.EnableAutostart();
			else
				AutostartHelper.DisableAutostart();
		}
	}
}