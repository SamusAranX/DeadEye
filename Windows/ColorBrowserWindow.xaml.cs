using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using DeadEye.Helpers;

namespace DeadEye.Windows;

public partial class ColorBrowserWindow : INotifyPropertyChanged
{
	public ColorBrowserWindow()
	{
		this.InitializeComponent();

		this.Tabs = new List<TabItemWrapper>(new[]
		{
			new TabItemWrapper("ImmersiveColors", GetImmersiveColors()),
			new TabItemWrapper("SystemColors", GetBrushes(typeof(SystemColors))),
			new TabItemWrapper("SystemParameters", GetBrushes(typeof(SystemParameters))),
			new TabItemWrapper("Brushes", GetBrushes(typeof(Brushes))),
		});

		this.OnPropertyChanged(nameof(this.Tabs));
	}

	public List<TabItemWrapper> Tabs { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged(string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private static IEnumerable<ColorWrapper> GetImmersiveColors()
	{
		var colorList = new List<ColorWrapper>();

		for (uint i = 0; i < 0xFFF; i++)
		{
			var typeNamePtr = UXTheme.GetImmersiveColorNamedTypeByIndex(i);
			if (typeNamePtr == nint.Zero)
				continue;

			var typeName = (nint)Marshal.PtrToStructure(typeNamePtr, typeof(nint))!;
			var colorName = Marshal.PtrToStringUni(typeName);
			var color = UXTheme.GetImmersiveColorByString($"Immersive{colorName}");

			colorList.Add(new ColorWrapper(colorName, new SolidColorBrush(color)));
		}

		return colorList;
	}

	private static IEnumerable<ColorWrapper> GetBrushes(Type type)
	{
		var properties = type.GetProperties().OrderBy(p => p.Name);

		var brushList = new List<ColorWrapper>();
		foreach (var propInfo in properties)
		{
			if (!propInfo.Name.EndsWith("Brush", StringComparison.InvariantCulture) && !propInfo.PropertyType.IsSubclassOf(typeof(Brush)))
				continue;

			var resName = propInfo.Name;
			if (resName.EndsWith("Brush", StringComparison.InvariantCulture))
				resName = resName.Remove(propInfo.Name.Length - 5);

			var brush = (Brush)propInfo.GetValue(null);

			brushList.Add(new ColorWrapper(resName, brush));
		}

		return brushList.ToArray();
	}
}
