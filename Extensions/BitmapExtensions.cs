using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DeadEye.Extensions;

public static class BitmapExtensions
{
	public static BitmapSource ToBitmapSource(this Bitmap bitmap)
	{
		var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

		var bitmapSource = BitmapSource.Create(bitmapData.Width, bitmapData.Height,
			bitmap.HorizontalResolution, bitmap.VerticalResolution, PixelFormats.Bgr24,
			null, bitmapData.Scan0,
			bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

		bitmap.UnlockBits(bitmapData);

		bitmapSource.Freeze();
		return bitmapSource;
	}
}
