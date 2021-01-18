using System.Windows;

namespace DeadEye.Extensions {
	public static class WindowExtension {
		public static void SetSize(this Window w, Rect r) {
			w.Top = r.Top;
			w.Left = r.Left;
			w.Width = r.Width;
			w.Height = r.Height;
		}
	}
}
