using System.IO;
using System.Reflection;

namespace DeadEye.Windows;

public partial class AboutWindow
{
	private string _appVersion;
	private string _gitVersion;

	public AboutWindow()
	{
		this.InitializeComponent();
	}

	public string AppVersion
	{
		get
		{
			if (this._appVersion != null)
				return this._appVersion;

			var version = Assembly.GetExecutingAssembly().GetName().Version;
			this._appVersion = version?.ToString() ?? "N/A";

			return this._appVersion;
		}
	}

	public string GitVersion
	{
		get
		{
			if (this._gitVersion != null)
				return this._gitVersion;

			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DeadEye.version.txt");
			using var reader = new StreamReader(stream ?? throw new InvalidOperationException("Couldn't load git version"));
			this._gitVersion = reader.ReadToEnd().Trim();

			return this._gitVersion;
		}
	}
}
