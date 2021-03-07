using System;

namespace DeadEye.Helpers {
	internal class GCHelper {
		public static void CleanUp() {
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}
	}
}
