using Godot;
using HarmonyLib;
using LaffeySpire2.LaffeySpire2Code.Characters;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace LaffeySpire2.LaffeySpire2Code.Patches;

/// <summary>
/// Restarts Laffey's idle animation after RitsuLib replaces the combat Spine skeleton.
/// </summary>
public sealed class LaffeyCombatSpineIdleBootstrapPatch : IPatchMethod
{
	public static string PatchId => "laffey_combat_spine_idle_bootstrap";

	public static string Description =>
		"Restart Laffey's combat idle animation after the selected Spine skeleton is ready";

	public static bool IsCritical => false;

	public static ModPatchTarget[] GetTargets() =>
		[new(typeof(NCreature), nameof(NCreature._Ready))];

	[HarmonyPostfix]
	[HarmonyPriority(Priority.Last)]
	public static void Postfix(NCreature __instance)
	{
		if (__instance.Entity?.Player?.Character is not LaffeyCharacter ||
		    __instance.Visuals?.SpineBody is not { } spineBody)
		{
			return;
		}

		// RitsuLib applies CombatSkeletonDataPath from an NCreature._Ready postfix.
		// That rebuilds the animation state and clears the idle track created earlier.
		Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(__instance) ||
			    !__instance.IsInsideTree() ||
			    __instance.Entity.IsDead ||
			    __instance.Visuals?.SpineBody != spineBody)
			{
				return;
			}

			__instance.RunWhenSpineReady(spineBody, _ =>
			{
				if (GodotObject.IsInstanceValid(__instance) &&
				    __instance.IsInsideTree() &&
				    !__instance.Entity.IsDead)
				{
					__instance.SetAnimationTrigger(CreatureAnimator.idleTrigger);
				}
			});
		}).CallDeferred();
	}
}
