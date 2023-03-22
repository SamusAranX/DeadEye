using System.Runtime.InteropServices;
using System.Windows.Media;

namespace DeadEye
{
	internal static class UXTheme
	{
		[DllImport("uxtheme.dll", EntryPoint = "#94")]
		internal static extern uint GetImmersiveColorSetCount();

		[DllImport("uxtheme.dll", EntryPoint = "#95")]
		internal static extern uint GetImmersiveColorFromColorSetEx(uint immersiveColorSet, uint immersiveColorType, bool ignoreHighContrast, uint highContrastCacheMode);

		[DllImport("uxtheme.dll", EntryPoint = "#96", CharSet = CharSet.Unicode)]
		internal static extern uint GetImmersiveColorTypeFromName(string name);

		[DllImport("uxtheme.dll", EntryPoint = "#98")]
		internal static extern uint GetImmersiveUserColorSetPreference(bool forceCheckRegistry, bool skipCheckOnFail);

		[DllImport("uxtheme.dll", EntryPoint = "#100")]
		internal static extern nint GetImmersiveColorNamedTypeByIndex(uint index);

		public static Color GetImmersiveColorByString(string key)
		{
			//Debug.WriteLine($"Requested color {key}");
			var colorSet = GetImmersiveUserColorSetPreference(false, false);
			var type = GetImmersiveColorTypeFromName(key);

			var colorUint = GetImmersiveColorFromColorSetEx(colorSet, type, false, 0);

			var color = new Color
			{
				A = (byte)((0xFF000000 & colorUint) >> 24),
				B = (byte)((0x00FF0000 & colorUint) >> 16),
				G = (byte)((0x0000FF00 & colorUint) >> 8),
				R = (byte)(0x000000FF & colorUint),
			};

			return color;
		}

		public static IEnumerable<Color> GetAllImmersiveColors()
		{
			var colorList = new List<Color>();
			for (uint i = 0; i < 0xFFF; i++)
			{
				var typeNamePtr = GetImmersiveColorNamedTypeByIndex(i);
				if (typeNamePtr == nint.Zero)
					continue;

				var typeName = (nint)Marshal.PtrToStructure(typeNamePtr, typeof(nint))!;
				var colorName = Marshal.PtrToStringUni(typeName);
				var color = GetImmersiveColorByString($"Immersive{colorName}");

				colorList.Add(color);
			}

			return colorList;
		}
	}
}
