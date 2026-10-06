using MegaCrit.Sts2.Core.Runs;

namespace LaffeySpire2.LaffeySpire2Code.Acts;

public static class SolomonSeaRestSiteContext
{
	[ThreadStatic]
	private static Stack<IRunState>? _runs;

	public static bool Enter(IRunState runState)
	{
		if (runState.Act is not SolomonSea)
			return false;

		(_runs ??= new()).Push(runState);
		return true;
	}

	public static bool IsActive(IRunState runState) => _runs?.Contains(runState) == true;

	public static void Exit(bool entered)
	{
		if (entered)
			_runs!.Pop();
	}
}
