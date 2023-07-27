namespace DeadEye.Helpers;

internal static class DebugHelper
{
#if DEBUG
	private const bool DEBUG_MODE = true;
#else
	private const bool DEBUG_MODE = false;
#endif

	public static bool IsDebugMode => DEBUG_MODE || AutostartHelper.AppPath.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase);
}
