using System.Globalization;
using System.Reflection;

namespace DeadEye.Windows;

public partial class AboutWindow
{
	public AboutWindow()
	{
		this.InitializeComponent();
	}

	public DateTime BuildDateTime => DateTime.Parse(Version.BUILD_TIME, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToLocalTime();

	public string Copyright
	{
		get
		{
			var asm = Assembly.GetExecutingAssembly();
			var obj = asm.GetCustomAttributes(false);
			foreach (var o in obj)
			{
				if (o is AssemblyCopyrightAttribute aca)
					return aca.Copyright;
			}

			return string.Empty;
		}
	}
}
