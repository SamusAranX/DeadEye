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
		GridType.GoldenRule,
	};

	private void SettingsWindow_OnClosing(object sender, CancelEventArgs e)
	{
		// Instead of saving after every little change, only write the settings to disk when the settings window closes
		Settings.Shared.WaitingForHotkey = null;
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

	private void CloseButton_OnClick(object sender, RoutedEventArgs e)
	{
		this.Close();
	}

	private void ScreenshotHotkeyButton_OnClick(object sender, RoutedEventArgs e)
	{
		if (Settings.Shared.WaitingForHotkey.HasValue)
			Settings.Shared.WaitingForHotkey = null;
		else
			Settings.Shared.WaitingForHotkey = HotkeyType.Screenshot;

		EnsureHotkeyStatus();
	}

	private void ColorPickerHotkeyButton_OnClick(object sender, RoutedEventArgs e)
	{
		if (Settings.Shared.WaitingForHotkey.HasValue)
			Settings.Shared.WaitingForHotkey = null;
		else
			Settings.Shared.WaitingForHotkey = HotkeyType.ColorPicker;

		EnsureHotkeyStatus();
	}

	private static void EnsureHotkeyStatus()
	{
		if (Settings.Shared.WaitingForHotkey.HasValue)
			HotkeyManager.Shared.UnregisterHotkeys();
		else
			HotkeyManager.Shared.RegisterHotkeys();

		Debug.WriteLine($"Screenshot Hotkey active: {HotkeyManager.Shared.IsScreenshotHotkeyRegistered}");
		Debug.WriteLine($"Color Picker Hotkey active: {HotkeyManager.Shared.IsColorPickerHotkeyRegistered}");
	}

	private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.Source is not TabControl || !Settings.Shared.WaitingForHotkey.HasValue)
			return;

		// cancel hotkey process when switching tabs
		Settings.Shared.WaitingForHotkey = null;
		EnsureHotkeyStatus();
	}

	private void SettingsWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (!Settings.Shared.WaitingForHotkey.HasValue)
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
			var newHotkey = new ShortcutKey(modifiers, key.Value);
			switch (Settings.Shared.WaitingForHotkey)
			{
				case HotkeyType.Screenshot:
					Settings.Shared.ScreenshotKey = newHotkey;
					break;
				case HotkeyType.ColorPicker:
					Settings.Shared.ColorPickerKey = newHotkey;
					break;
			}

			Settings.Shared.WaitingForHotkey = null;
		}

		EnsureHotkeyStatus();
	}
}
