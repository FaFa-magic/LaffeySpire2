using HarmonyLib;
using LaffeySpire2.LaffeySpire2Code.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Patching.Models;

namespace LaffeySpire2.LaffeySpire2Code.Patches;

public sealed class VulnerablePowerDescriptionPatch : IPatchMethod
{
	public static string PatchId => "laffey_trapped_in_the_enemy_vulnerable_description";
	public static string Description => "Show outgoing damage on Vulnerable while Trapped in the Enemy is active";
	public static bool IsCritical => true;

	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(PowerModel), nameof(PowerModel.SmartDescription), null, MethodType.Getter)
	];

	[HarmonyPostfix]
	public static void Postfix(PowerModel __instance, ref LocString __result)
	{
		if (__instance is VulnerablePower &&
			__instance.IsMutable &&
			__instance.Owner.HasPower<TrappedInTheEnemyPower>())
		{
			__result = new LocString(
				"powers",
				"LAFFEY_SPIRE2_POWER_TRAPPED_IN_THE_ENEMY_POWER.vulnerableSmartDescription");
		}
	}
}
