using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Serialization;

namespace DeadEye {
	public enum GridType {
		[Description("None")]
		None,

		[Description("Rule of Thirds")]
		RuleOfThirds,
	}

	[Serializable]
	public class Settings: INotifyPropertyChanged {

		#region "Singleton"

		[NonSerialized]
		private static Settings _sharedSettings;
		public static Settings SharedSettings {
			get {
				if (_sharedSettings == null)
					_sharedSettings = Load();

				return _sharedSettings;
			}

			set => _sharedSettings = value;
		}

		#endregion

		#region "INotifyPropertyChanged"

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged([CallerMemberName] string propertyName = null) {
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

		private GridType _gridType = GridType.None;
		private bool _fastScreenshot = true;
		private bool _markCenter = false;
		private bool _showDimensions = false;
		private double _textSize = 11;

		#region "Properties for Bindings"

		public GridType GridType {
			get => this._gridType;
			set {
				this._gridType = value;
				this.OnPropertyChanged();
			}
		}

		public bool FastScreenshot {
			get => this._fastScreenshot;
			set {
				this._fastScreenshot = value;
				this.OnPropertyChanged();
			}
		}

		public bool MarkCenter {
			get => this._markCenter;
			set {
				this._markCenter = value;
				this.OnPropertyChanged();
			}
		}

		public bool ShowDimensions {
			get => this._showDimensions;
			set {
				this._showDimensions = value;
				this.OnPropertyChanged();
			}
		}

		public double TextSize {
			get => this._textSize;
			set {
				this._textSize = value;
				this.OnPropertyChanged();
			}
		}

		#endregion

		#region "Loading and Saving"

		private static readonly string _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeadEye", "settings.xml");

		public static Settings Load() {
			if (!File.Exists(_settingsPath))
				return new Settings();

			try {
				using var stream = new FileStream(_settingsPath, FileMode.Open);
				var readerSettings = new XmlReaderSettings {
					DtdProcessing = DtdProcessing.Ignore
				};
				using var reader = XmlReader.Create(stream, readerSettings);

				var x = new XmlSerializer(typeof(Settings));
				return (Settings)x.Deserialize(reader);
			} catch (Exception e) {
				Debug.WriteLine("Error while loading settings");
				Debug.WriteLine(e);
				var s = new Settings();
				s.Save();
				return s;
			}
		}

		public void Save() {
			var settingsDir = Path.GetDirectoryName(_settingsPath);
			if (settingsDir == null)
				throw new DirectoryNotFoundException("no");

			Directory.CreateDirectory(settingsDir);

			try {
				using var s = new FileStream(_settingsPath, FileMode.Create);
				var x = new XmlSerializer(typeof(Settings));
				x.Serialize(s, this);
			} catch (Exception e) {
				Debug.WriteLine("Error while saving settings");
				Debug.WriteLine(e);
			}
		}

		#endregion
	}
}
