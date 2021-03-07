using System;
using System.Threading;

namespace DeadEye.Helpers {
	internal class SingleInstanceHelper {
		private static readonly Mutex mutex = new Mutex(true, "{755d1423-1f2d-4267-a282-3f5a8ccde350}");

		public static bool IsOtherInstanceRunning() {
			return !mutex.WaitOne(TimeSpan.Zero, true);
		}

		public static void Release() {
			mutex.ReleaseMutex();
		}
	}
}
