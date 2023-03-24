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
	private readonly ModifierKeys[] _allModifiers = { ModifierKeys.Control, ModifierKeys.Shift, ModifierKeys.Alt, ModifierKeys.Windows };
	private readonly Key[] _ignoredKeys = { Key.System, Key.LeftCtrl, Key.RightCtrl, Key.LeftShift, Key.RightShift, Key.LeftAlt, Key.RightAlt, Key.LWin, Key.RWin };

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
		RestoreHotkey();
	}

	private static void RestoreHotkey()
	{
		if (Settings.Shared.WaitingForHotkey)
			HotkeyManager.Shared.UnregisterHotkey();
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
		var key = e.Key;

		if (key == Key.System)
			key = e.SystemKey;

		var modList = new List<string>();
		foreach (var modifier in this._allModifiers)
		{
			if (modifiers.HasFlag(modifier))
				modList.Add(modifier.ToString().ToUpperInvariant());
		}

		if (key != Key.Escape)
		{
			if (modList.Count > 0)
				Debug.WriteLine($"{string.Join("+", modList)} - {key}");
			else
				Debug.WriteLine("key");

			// todo: logic goes here
			Settings.Shared.ScreenshotModifierKeys = modifiers;
			Settings.Shared.ScreenshotKey = key;
		}

		if (!this._ignoredKeys.Contains(key))
			Settings.Shared.WaitingForHotkey = false;

		RestoreHotkey();
	}
}
