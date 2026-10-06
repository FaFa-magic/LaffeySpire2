using HarmonyLib;
using LaffeySpire2.LaffeySpire2Code.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace LaffeySpire2.LaffeySpire2Code.Acts;

public static class SolomonSeaRun
{
	private static readonly Action<RunState, IReadOnlyList<ActModel>> SetActs =
		AccessTools.PropertySetter(typeof(RunState), nameof(RunState.Acts))
			.CreateDelegate<Action<RunState, IReadOnlyList<ActModel>>>();

	public static void EnsureAct(RunState? runState)
	{
		if (runState == null || runState.Acts.Count != 3 || runState.CurrentActIndex > 2
		    || runState.CurrentRoom?.IsVictoryRoom == true
		    || !runState.Players.Any(player => player.Character is LaffeyCharacter))
			return;

		SetActs(runState, [.. runState.Acts, SolomonSea.CreateForRun()]);
	}
}
