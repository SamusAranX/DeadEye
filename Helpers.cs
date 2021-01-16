using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DeadEye.Extensions;

namespace DeadEye {
	class Helpers {
		public static Rect GetVirtualScreenRect() => new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);

		public static Bitmap GetFullscreenScreenshotGDI() {
			var rect = Helpers.GetVirtualScreenRect().ToRectangle();
			Debug.WriteLine($"Virtual Screen rect: {rect}");

			var bm = new Bitmap(rect.Width, rect.Height);
			var g = Graphics.FromImage(bm);
			g.CopyFromScreen(rect.X, rect.Y, 0, 0, bm.Size);

			return bm;
		}
	}
}
