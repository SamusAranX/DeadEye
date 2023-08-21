using System.Globalization;
using System.Windows.Input;

namespace DeadEye;

public sealed class ShortcutKey
{
	public static readonly ModifierKeys[] AllModifiers = { ModifierKeys.Control, ModifierKeys.Shift, ModifierKeys.Alt, ModifierKeys.Windows };

	public static readonly Key[] IgnoredKeys =
	{
		// do not put Key.Escape in here
		Key.System,
		Key.LeftCtrl, Key.RightCtrl,
		Key.LeftShift, Key.RightShift,
		Key.LeftAlt, Key.RightAlt,
		Key.LWin, Key.RWin,
		Key.NumLock, Key.CapsLock, Key.Scroll,
		Key.Apps, Key.Sleep,
	};

	private static readonly Dictionary<ModifierKeys, string> MODIFIER_NAMES = new()
	{
		{ ModifierKeys.Control, "Ctrl" },
		{ ModifierKeys.Shift, "Shift" },
		{ ModifierKeys.Alt, "Alt" },
		{ ModifierKeys.Windows, "Win" },
	};

	private static readonly Dictionary<Key, string> KEY_NAMES = new()
	{
		{ Key.PageUp, "Page Up" },
		{ Key.PageDown, "Page Down" },
		{ Key.Back, "Backspace" },
		{ Key.Enter, "Enter" },
		{ Key.PrintScreen, "PrtScr" },
	};

	[Obsolete("Only used by the serializer", true)]
	public ShortcutKey() { }

	public ShortcutKey(ModifierKeys modifiers, Key key)
	{
		this.ModifierKeys = modifiers;
		this.Key = key;
	}

	public ModifierKeys ModifierKeys { get; set; }

	public Key Key { get; set; }

	public override string ToString()
	{
		if (this.Key == Key.None)
			return "None";

		var keyList = new List<string>();
		foreach (var modifier in AllModifiers)
		{
			if (this.ModifierKeys.HasFlag(modifier))
				keyList.Add(MODIFIER_NAMES[modifier]);
		}

		if (!IgnoredKeys.Contains(this.Key))
		{
			var keyName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(this.Key.ToString());
			if (KEY_NAMES.TryGetValue(this.Key, out var value))
				keyName = value;
			else if (this.Key is >= Key.D0 and <= Key.D9)
				keyName = (this.Key - Key.D0).ToString(CultureInfo.InvariantCulture);
			else if (this.Key is >= Key.NumPad0 and <= Key.NumPad9)
				keyName = "Numpad " + (this.Key - Key.NumPad0).ToString(CultureInfo.InvariantCulture);

			keyList.Add(keyName);
		}

		return string.Join("+", keyList);
	}
}
