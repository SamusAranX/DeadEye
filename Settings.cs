using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DeadEye {
	public enum GridType {
		[Description("None")]
		None,

		[Description("Rule of Thirds")]
		RuleOfThirds,
	}

	[Serializable]
	public class Settings {

		private static readonly string _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeadEye", "settings.bin");

		public GridType GridType { get; set; }
		public bool FastScreenshot { get; set; }
		public byte ThresholdSize { get; set; }

		public Settings() {
			this.GridType = GridType.None;
			this.FastScreenshot = true;
			this.ThresholdSize = 14;
		}

		private static Settings _sharedSettings;
		public static Settings SharedSettings {
			get {
				if (_sharedSettings == null)
					_sharedSettings = Settings.Load();

				return _sharedSettings;
			}

			set => _sharedSettings = value;
		}

		public static Settings Load() {
			if (!File.Exists(_settingsPath))
				return new Settings();

			try {
				using var s = new FileStream(_settingsPath, FileMode.Open);
				var b = new BinaryFormatter();
				return (Settings)b.Deserialize(s);
			} catch (Exception e) {
				Debug.WriteLine("Error while loading settings");
				Debug.WriteLine(e);
				return new Settings();
			}
		}

		public void Save() {
			var settingsDir = Path.GetDirectoryName(_settingsPath);
			if (settingsDir == null)
				throw new NullReferenceException("no");
				
			Directory.CreateDirectory(settingsDir);

			try {
				using var s = new FileStream(_settingsPath, FileMode.Create);
				var b = new BinaryFormatter();
				b.Serialize(s, this);
			} catch (Exception e) {
				Debug.WriteLine("Error while saving settings");
				Debug.WriteLine(e);
			}
		}
	}
}
