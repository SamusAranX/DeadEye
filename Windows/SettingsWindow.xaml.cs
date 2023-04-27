using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DeadEye.Helpers;
using DeadEye.Hotkeys;

namespace DeadEye.Windows;

public partial class SettingsWindow
{
	public SettingsWindow()
	{
		this.InitializeComponent();
	}

	public static GridType[] GridTypes => new[]
	{
		GridType.None,
		GridType.RuleOfThirds,
	};

	private void SettingsWindow_OnClosing(object sender, CancelEventArgs e)
	{
		// Instead of saving after every little change, save when the settings window closes
		Settings.Shared.WaitingForHotkey = false;
		Settings.Shared.Save();
	}

	private void LoadAutostartStatus(object sender, EventArgs e)
	{
		Settings.Shared.AutostartStatus = AutostartHelper.GetTaskmgrAutostartStatus();
		Debug.WriteLine($"Autostart: {Settings.Shared.AutostartStatus}");
	}

	private void AutostartCheckBox_OnCheckUncheck(object sender, RoutedEventArgs e)
	{
		if (!this.IsLoaded)
			return;

		var checkbox = (CheckBox)sender;
		if (checkbox.IsChecked.HasValue && checkbox.IsChecked.Value)
			AutostartHelper.EnableAutostart();
		else
			AutostartHelper.DisableAutostart();
	}

	private void HotkeyButton_OnClick(object sender, RoutedEventArgs e)
	{
		Settings.Shared.WaitingForHotkey = !Settings.Shared.WaitingForHotkey;
		EnsureHotkeyStatus();
	}

	private static void EnsureHotkeyStatus()
	{
		if (Settings.Shared.WaitingForHotkey)
		{
			HotkeyManager.Shared.UnregisterHotkey();
		}
		else
			HotkeyManager.Shared.RegisterHotkey();

		Debug.WriteLine($"Hotkey active: {HotkeyManager.Shared.IsHotkeyRegistered}");
	}

	private void SettingsWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (!Settings.Shared.WaitingForHotkey)
			return;

		e.Handled = true;

		var modifiers = Keyboard.Modifiers;
		Key? key = e.Key;

		if (key == Key.System)
			key = e.SystemKey;

		if (ShortcutKey.IgnoredKeys.Contains(key.Value))
			key = null;

		Debug.WriteLine($"key: {key}");

		// if key is not null here, we may have found our new hotkey
		if (key.HasValue && key != Key.Escape)
		{
			// we actually have a new hotkey
			Settings.Shared.ScreenshotKey = new ShortcutKey(modifiers, key.Value);
			Settings.Shared.WaitingForHotkey = false;
		}

		EnsureHotkeyStatus();
	}
}
