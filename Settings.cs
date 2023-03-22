using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Serialization;
using DeadEye.Helpers;

namespace DeadEye;

public enum GridType
{
	[Description("None")]
	None,

	[Description("Rule of Thirds")]
	RuleOfThirds,
}

public sealed class Settings : INotifyPropertyChanged
{
	private bool _autostartEnabled;
	private AutostartStatus _autostartStatus;
	private bool _fastScreenshot = true;

	private GridType _gridType = GridType.None;
	private bool _markCenter;
	private bool _showDimensions;
	private double _textSize = 11;

	public Settings()
	{
		this._autostartEnabled = AutostartHelper.CheckAutostartStatus();
	}

	#region "Singleton"

	private static Settings _sharedSettings;

	public static Settings SharedSettings
	{
		get { return _sharedSettings ??= Load(); }

		set => _sharedSettings = value;
	}

	#endregion

	#region "INotifyPropertyChanged"

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion

	#region "Properties for Bindings"

	public GridType GridType
	{
		get => this._gridType;
		set
		{
			this._gridType = value;
			this.OnPropertyChanged();
		}
	}

	public bool FastScreenshot
	{
		get => this._fastScreenshot;
		set
		{
			this._fastScreenshot = value;
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

	#endregion

	#region "Loading and Saving"

	private static readonly string SETTINGS_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeadEye", "settings.xml");

	public static Settings Load()
	{
		if (!File.Exists(SETTINGS_PATH))
			return new Settings();

		try
		{
			using var stream = new FileStream(SETTINGS_PATH, FileMode.Open);
			var readerSettings = new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Ignore,
			};
			using var reader = XmlReader.Create(stream, readerSettings);

			var x = new XmlSerializer(typeof(Settings));
			return (Settings)x.Deserialize(reader);
		}
		catch (Exception e)
		{
			Debug.WriteLine("Error while loading settings");
			Debug.WriteLine(e);
			var s = new Settings();
			s.Save();
			return s;
		}
	}

	public void Save()
	{
		var settingsDir = Path.GetDirectoryName(SETTINGS_PATH);
		if (settingsDir == null)
			throw new DirectoryNotFoundException("no");

		Directory.CreateDirectory(settingsDir);

		try
		{
			using var s = new FileStream(SETTINGS_PATH, FileMode.Create);
			var x = new XmlSerializer(typeof(Settings));
			x.Serialize(s, this);
		}
		catch (Exception e)
		{
			Debug.WriteLine("Error while saving settings");
			Debug.WriteLine(e);
		}
	}

	#endregion
}
