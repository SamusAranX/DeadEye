using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using DeadEye.Extensions;
using DeadEye.Win32;

namespace DeadEye.Helpers;

internal sealed class ScreenshotHelper
{
	public static Rect GetVirtualScreenRect()
	{
		return SystemInformation.VirtualScreen;
	}

	public static Rect GetVirtualScreenRectNormalized()
	{
		var r = GetVirtualScreenRect();
		r.X = 0;
		r.Y = 0;
		return r;
	}

	public static Rect GetPrimaryScreenRect()
	{
		return Screen.PrimaryScreen?.Bounds ?? Rect.Empty;
	}

	public static Bitmap GetFullscreenScreenshotGDI()
	{
		var rect = GetVirtualScreenRect().ToRectangle();

		var bm = new Bitmap(rect.Width, rect.Height, PixelFormat.Format24bppRgb);
		using var g = Graphics.FromImage(bm);

		g.CopyFromScreen(rect.X, rect.Y, 0, 0, bm.Size, CopyPixelOperation.SourceCopy);

		return bm;
	}
}
