namespace DeadEye.Windows {
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow {

		public GridType[] GridTypes {
			get {
				return new[]
				{
					GridType.None,
					GridType.RuleOfThirds,
				};
			}
		}

		public SettingsWindow() {
			this.InitializeComponent();
		}
	}
}
