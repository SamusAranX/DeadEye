using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using DeadEye.Helpers;
using DeadEye.Hotkeys;

namespace DeadEye;

public enum GridType
{
	[Description("None")]
	None,

	[Description("Rule of Thirds")]
	RuleOfThirds,

	[Description("Golden Rule")]
	GoldenRule,
}

public sealed class Settings : INotifyPropertyChanged
{
	private bool _autostartEnabled = AutostartHelper.CheckAutostartStatus();
	private AutostartStatus _autostartStatus;

	private GridType _gridType = GridType.None;
	private bool _markCenter;
	private bool _showDimensions;
	private double _textSize = 11;
	
	private double _pickerRadius = 72;
	private bool _allColorLabel = true;

	private ShortcutKey? _screenshotKey = new(ModifierKeys.Shift | ModifierKeys.Alt, Key.D4);
	private ShortcutKey? _colorPickerKey = new(ModifierKeys.Shift | ModifierKeys.Alt, Key.C);

	private HotkeyType? _waitingForHotkey;

	#region Singleton

	private static Settings? _sharedSettings;

	public static Settings Shared
	{
		get => _sharedSettings ??= Load();
		set => _sharedSettings = value;
	}

	#endregion

	#region INotifyPropertyChanged

	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion

	#region Properties for Bindings

	public GridType GridType
	{
		get => this._gridType;
		set
		{
			this._gridType = value;
			this.OnPropertyChanged();
		}
	}

	public bool MarkCenter
	{
		get => this._markCenter;
		set
		{
			this._markCenter = value;
			this.OnPropertyChanged();
		}
	}

	public bool ShowDimensions
	{
		get => this._showDimensions;
		set
		{
			this._showDimensions = value;
			this.OnPropertyChanged();
		}
	}

	public double TextSize
	{
		get => this._textSize;
		set
		{
			this._textSize = value;
			this.OnPropertyChanged();
		}
	}

	public double PickerRadius
	{
		get => this._pickerRadius;
		set
		{
			this._pickerRadius = value;
			this.OnPropertyChanged();
		}
	}

	public bool AllColorLabel
	{
		get => this._allColorLabel;
		set
		{
			this._allColorLabel = value;
			this.OnPropertyChanged();
		}
	}

	[XmlIgnore]
	public bool AutostartEnabled
	{
		get => this._autostartEnabled;
		set
		{
			this._autostartEnabled = value;
			this.OnPropertyChanged();
		}
	}

	[XmlIgnore]
	public AutostartStatus AutostartStatus
	{
		get => this._autostartStatus;
		set
		{
			this._autostartStatus = value;
			this.OnPropertyChanged();
		}
	}

	[XmlIgnore]
	public HotkeyType? WaitingForHotkey
	{
		get => this._waitingForHotkey;
		set
		{
			this._waitingForHotkey = value;
			this.OnPropertyChanged();
		}
	}

	public ShortcutKey? ScreenshotKey
	{
		get => this._screenshotKey;
		set
		{
			this._screenshotKey = value;
			this.OnPropertyChanged();
		}
	}

	public ShortcutKey? ColorPickerKey
	{
		get => this._colorPickerKey;
		set
		{
			this._colorPickerKey = value;
			this.OnPropertyChanged();
		}
	}

	#endregion

	#region Loading and Saving

	private static readonly string SETTINGS_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeadEye", "settings.xml");

	public static Settings Load()
	{
		if (!File.Exists(SETTINGS_PATH))
		{
			Debug.WriteLine("Creating new settings file");
			var s = new Settings();
			s.Save();
			return s;
		}

		try
		{
			using var stream = new FileStream(SETTINGS_PATH, FileMode.Open);
			var readerSettings = new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Ignore,
			};
			using var reader = XmlReader.Create(stream, readerSettings);

			var x = new XmlSerializer(typeof(Settings));
			var xd = x.Deserialize(reader);

			if (xd == null)
				throw new InvalidDataException("Can't deserialize settings file");

			var settings = (Settings)xd;
			if (settings.ScreenshotKey == settings.ColorPickerKey)
			{
				settings.ScreenshotKey = null;
				settings.ColorPickerKey = null;
				MessageBox.Show("All shortcut keys were reset because of conflicts. Please reassign the shortcut keys in the Settings.");
			}

			return settings;
		}
		catch (Exception e)
		{
			Debug.WriteLine("Error while loading settings:\n{0}", e);
			var s = new Settings();
			s.Save();
			return s;
		}
	}

	public void Save()
	{
		var settingsDir = Path.GetDirectoryName(SETTINGS_PATH);
		if (settingsDir == null)
			throw new DirectoryNotFoundException("Can't find settings directory.");

		Directory.CreateDirectory(settingsDir);

		var writerSettings = new XmlWriterSettings
		{
			Indent = true,
			IndentChars = "\t",
			NewLineChars = "\n",
			OmitXmlDeclaration = true,
		};

		try
		{
			using var s = new FileStream(SETTINGS_PATH, FileMode.Create);
			using var writer = XmlWriter.Create(s, writerSettings);

			var x = new XmlSerializer(typeof(Settings));
			x.Serialize(writer, this);
		}
		catch (Exception e)
		{
			Debug.WriteLine("Error while saving settings");
			Debug.WriteLine(e);
		}
	}

	#endregion
}
