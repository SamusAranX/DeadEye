namespace DeadEye.Helpers;

internal sealed class GCHelper
{
	public static void CleanUp()
	{
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
		GC.WaitForPendingFinalizers();
	}
}
