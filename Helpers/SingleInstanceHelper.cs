using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace DeadEye.Helpers {
	internal class SingleInstanceHelper: IDisposable {
		private readonly Mutex _mutex;
		public SingleInstanceHelper() {
			var appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), false).GetValue(0)).Value;
			var mutexId = $"Global\\{{{appGuid}}}";
			this._mutex = new Mutex(false, mutexId);
		}

		public bool IsOtherInstanceRunning(int millisecondsTimeout = 100) {
			return !this._mutex.WaitOne(millisecondsTimeout, false);
		}

		public void Dispose() {
			this._mutex.Close();
			this._mutex.Dispose();
		}
	}
}
