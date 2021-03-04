using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using DeadEye.Extensions;

namespace DeadEye.Helpers {
	internal class ScreenshotHelper {
		public static Rect GetVirtualScreenRect() {
			return new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
		}

		public static Bitmap GetFullscreenScreenshotGDI() {
			var rect = GetVirtualScreenRect().ToRectangle();

			var bm = new Bitmap(rect.Width, rect.Height, PixelFormat.Format24bppRgb);
			using var g = Graphics.FromImage(bm);
			
			g.CopyFromScreen(rect.X, rect.Y, 0, 0, bm.Size, CopyPixelOperation.SourceCopy);

			return bm;
		}
	}
}
