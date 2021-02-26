using System;
using System.Deployment.Application;
using System.IO;
using System.Reflection;

namespace DeadEye.Windows {
	/// <summary>
	/// Interaction logic for AboutWindow.xaml
	/// </summary>
	public partial class AboutWindow {
		public AboutWindow() {
			//AeroBlurHelper.EnableBlur(this);

			this.InitializeComponent();

		}

		public string CurrentVersion {
			get {
				var version = ApplicationDeployment.IsNetworkDeployed
					? ApplicationDeployment.CurrentDeployment.CurrentVersion
					: Assembly.GetExecutingAssembly().GetName().Version;

				return version.ToString(3);
			}
		}

		private string _gitVersion;
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
