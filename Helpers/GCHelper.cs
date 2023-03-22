namespace DeadEye.Helpers;

internal sealed class GCHelper
{
	public static void CleanUp()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}
}
