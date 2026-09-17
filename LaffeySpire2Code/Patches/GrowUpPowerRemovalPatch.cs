using HarmonyLib;
using LaffeySpire2.LaffeySpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Patching.Models;

namespace LaffeySpire2.LaffeySpire2Code.Patches;

public sealed class GrowUpPowerRemovalPatch : IPatchMethod
{
	public static string PatchId => "laffey_grow_up_direct_power_removal";
	public static string Description => "Trigger Grow Up when a debuff is removed directly";
	public static bool IsCritical => true;

	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(PowerCmd), nameof(PowerCmd.Remove), [typeof(PowerModel)])
	];

	[HarmonyPrefix]
	public static void Prefix(PowerModel? power, out Creature? __state)
	{
		__state = power is { IsMutable: true } &&
			power.TypeForCurrentAmount == PowerType.Debuff &&
			!power.ShouldRemoveDueToAmount()
			? power.Owner
			: null;
	}

	[HarmonyPostfix]
	public static void Postfix(Creature? __state, ref Task __result)
	{
		if (__state != null)
		{
			__result = AfterRemoval(__result, __state);
		}
	}

	private static async Task AfterRemoval(Task originalTask, Creature owner)
	{
		await originalTask;
		GrowUpPower? growUp = owner.GetPower<GrowUpPower>();
		if (growUp != null)
		{
			await PowerCmd.Apply<StrengthPower>(
				new ThrowingPlayerChoiceContext(),
				owner,
				growUp.Amount,
				owner,
				null);
		}
	}
}
