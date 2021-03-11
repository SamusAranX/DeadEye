using System;
using System.Deployment.Application;
using System.IO;
using System.Reflection;

namespace DeadEye.Windows {
	public partial class AboutWindow {
		private string _appVersion;
		private string _gitVersion;

		public AboutWindow() {
			this.InitializeComponent();
		}

		public string AppVersion {
			get {
				if (this._appVersion == null) {
					var version = ApplicationDeployment.IsNetworkDeployed
						? ApplicationDeployment.CurrentDeployment.CurrentVersion
						: Assembly.GetExecutingAssembly().GetName().Version;
					this._appVersion = version.ToString();
				}

				return this._appVersion;
			}
		}

		public string GitVersion {
			get {
				if (this._gitVersion == null) {
					using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DeadEye.version.txt");
					using var reader = new StreamReader(stream ?? throw new InvalidOperationException("Couldn't load git version"));
					this._gitVersion = reader.ReadToEnd().Trim();
				}

				return this._gitVersion;
			}
		}
	}
}