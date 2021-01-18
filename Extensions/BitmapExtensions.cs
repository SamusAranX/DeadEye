﻿using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace DeadEye.Extensions {
	public static class BitmapExtensions {
		public static BitmapImage ToBitmapImage(this Image img) {
			using var memory = new MemoryStream();

			img.Save(memory, ImageFormat.Png);
			memory.Position = 0;

			var bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.StreamSource = memory;
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			bitmapImage.Freeze();

			return bitmapImage;
		}
	}
}
