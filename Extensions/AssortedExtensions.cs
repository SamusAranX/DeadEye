using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DeadEye.Extensions {
	public static class AssortedExtensions {
		public static Rectangle ToRectangle(this Rect r) => new Rectangle((int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height);

		public static Int32Rect ToInt32Rect(this Rect r) => new Int32Rect((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
	}
}
