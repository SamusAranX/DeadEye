namespace DeadEye.Helpers;

internal sealed class SingleInstanceHelper : IDisposable
{
	private readonly Mutex _mutex;

	public SingleInstanceHelper(string mutexName)
	{
		var mutexId = @"Global\" + mutexName;
		this._mutex = new Mutex(false, mutexId);
	}

	public void Dispose()
	{
		this._mutex.Close();
		this._mutex.Dispose();
	}

	public bool IsOtherInstanceRunning(int millisecondsTimeout = 100)
	{
		return !this._mutex.WaitOne(millisecondsTimeout, false);
	}
}
