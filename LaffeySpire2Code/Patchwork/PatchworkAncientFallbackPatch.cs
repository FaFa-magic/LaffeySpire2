using HarmonyLib;
using LaffeySpire2.LaffeySpire2Code.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public sealed class PatchworkAncientFallbackPatch : IPatchMethod
{
	public static string PatchId => "laffey_patchwork_ancient_fallback";
	public static string Description => "Allow Laffey to receive ancient starter upgrades after all starters are gone";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(ArchaicTooth), nameof(ArchaicTooth.AfterObtained), []),
		new(typeof(TouchOfOrobas), nameof(TouchOfOrobas.AfterObtained), [])
	];

	[HarmonyPrefix]
	public static bool Prefix(RelicModel __instance, ref Task __result)
	{
		if (__instance.Owner.Character is not LaffeyCharacter)
			return true;
		if (__instance is ArchaicTooth { StarterCard: null } or TouchOfOrobas { StarterRelic: null })
		{
			__result = Task.CompletedTask;
			return false;
		}
		return true;
	}
}
