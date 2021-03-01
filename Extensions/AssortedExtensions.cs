using System.Windows;

using Rectangle = System.Drawing.Rectangle;

namespace DeadEye.Extensions {
	public static class AssortedExtensions {
		public static Rectangle ToRectangle(this Rect r) => new Rectangle((int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height);

		public static Int32Rect ToInt32Rect(this Rect r) => new Int32Rect((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
	}
}
