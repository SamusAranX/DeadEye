using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using DeadEye.Helpers;

namespace DeadEye.Windows {
	public partial class SettingsWindow {

		public static GridType[] GridTypes => new[] {
			GridType.None,
			GridType.RuleOfThirds
		};

		public SettingsWindow() {
			this.InitializeComponent();
		}

		private void SettingsWindow_OnClosing(object sender, CancelEventArgs e) {
			// Instead of saving after every little change, save when the settings window closes
			Settings.SharedSettings.Save();
		}

		private void SettingsWindow_OnSourceInitialized(object sender, EventArgs e) {
			var status = AutostartHelper.GetTaskmgrAutostartStatus();
			Settings.SharedSettings.AutostartStatus = status;
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