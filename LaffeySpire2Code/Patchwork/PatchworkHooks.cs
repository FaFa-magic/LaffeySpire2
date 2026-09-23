using HarmonyLib;
using LaffeySpire2.LaffeySpire2Code.Characters;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Patching.Models;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public sealed class PatchworkCombatStartPatch : IPatchMethod
{
	public static string PatchId => "laffey_patchwork_combat_start";
	public static string Description => "Grant completed patchwork combat-start milestones";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(Hook), nameof(Hook.BeforeCombatStart), [typeof(IRunState), typeof(ICombatState)])
	];

	[HarmonyPostfix]
	public static void Postfix(IRunState runState, ref Task __result) => __result = After(__result, runState);

	private static async Task After(Task original, IRunState runState)
	{
		await original;
		foreach (Player player in runState.Players)
		{
			if (player.Character is not LaffeyCharacter)
				continue;
			PatchworkSaveData state = PatchworkBoard.Get(player);
			if (state.ClaimedSquares.Contains(3))
				await PowerCmd.Apply<VigorPower>(new ThrowingPlayerChoiceContext(), player.Creature, 3,
					player.Creature, null);
			if (state.ClaimedSquares.Contains(4))
				await CreatureCmd.GainBlock(player.Creature, 6, ValueProp.Unpowered, null);
			if (state.ClaimedSquares.Contains(5))
				await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), player.Creature, 1,
					player.Creature, null);
			if (state.ClaimedSquares.Contains(6))
				await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), player.Creature, 1,
					player.Creature, null);
		}
	}
}

public sealed class PatchworkRewardPatch : IPatchMethod
{
	public static string PatchId => "laffey_patchwork_combat_reward";
	public static string Description => "Add an unclaimed patchwork piece to Laffey combat rewards";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(RewardsSet), nameof(RewardsSet.WithRewardsFromRoom), [typeof(AbstractRoom)])
	];

	[HarmonyPostfix]
	public static void Postfix(RewardsSet __instance, AbstractRoom room)
	{
		if (room is not CombatRoom combatRoom || __instance.Player.Character is not LaffeyCharacter ||
			combatRoom.Encounter?.ShouldGiveRewards == false ||
			(room.RoomType == RoomType.Boss &&
			 __instance.Player.RunState.CurrentActIndex >= __instance.Player.RunState.Acts.Count - 1))
			return;
		int pieceId = PatchworkBoard.RandomUnclaimedPiece(__instance.Player);
		if (pieceId > 0)
			__instance.Rewards.Add(new PatchworkPieceReward(__instance.Player, pieceId));
	}
}

public sealed class PatchworkDrawPatch : IPatchMethod
{
	public static string PatchId => "laffey_patchwork_draw";
	public static string Description => "Draw two extra cards each turn after completing a 9x9 square";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(Hook), nameof(Hook.ModifyHandDraw),
			[typeof(ICombatState), typeof(Player), typeof(decimal), typeof(IEnumerable<MegaCrit.Sts2.Core.Models.AbstractModel>).MakeByRefType()])
	];

	[HarmonyPostfix]
	public static void Postfix(Player player, ref decimal __result)
	{
		if (player.Character is LaffeyCharacter && PatchworkBoard.Get(player).ClaimedSquares.Contains(9))
			__result += 2;
	}
}

public sealed class PatchworkEnergyPatch : IPatchMethod
{
	public static string PatchId => "laffey_patchwork_energy";
	public static string Description => "Gain one energy each turn after completing a 10x10 square";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(Hook), nameof(Hook.AfterEnergyReset), [typeof(ICombatState), typeof(Player)])
	];

	[HarmonyPostfix]
	public static void Postfix(Player player, ref Task __result) => __result = After(__result, player);

	private static async Task After(Task original, Player player)
	{
		await original;
		if (player.Character is LaffeyCharacter && PatchworkBoard.Get(player).ClaimedSquares.Contains(10))
			await PlayerCmd.GainEnergy(1, player);
	}
}
