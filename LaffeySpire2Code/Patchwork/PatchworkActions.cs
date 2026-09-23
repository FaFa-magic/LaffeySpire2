using System.Text.Json;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Networking.ManagedActions;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public sealed record PatchworkActionPayload(int Kind, int PieceId, int X, int Y, int Rotation, bool Flipped, int AncientChoice);

public static class PatchworkActions
{
	private static readonly RitsuLibManagedNetActionDescriptor<PatchworkActionPayload> Descriptor = new(
		MainFile.ModId,
		"patchwork_place_or_buy",
		payload => JsonSerializer.SerializeToUtf8Bytes(payload),
		bytes => JsonSerializer.Deserialize<PatchworkActionPayload>(bytes)!,
		Execute,
		GameActionType.NonCombat);

	public static void Register() => RitsuLibManagedNetActions.Register(Descriptor);

	public static bool RequestPlacement(Player player, PatchworkPlacement placement, int ancientChoice) =>
		RitsuLibManagedNetActions.Request(RunManager.Instance, Descriptor,
			new PatchworkActionPayload(0, placement.PieceId, placement.X, placement.Y,
				placement.Rotation, placement.Flipped, ancientChoice), player.NetId);

	public static bool RequestShopPurchase(Player player) =>
		RitsuLibManagedNetActions.Request(RunManager.Instance, Descriptor,
			new PatchworkActionPayload(1, 0, 0, 0, 0, false, 0), player.NetId);

	public static string ShopKey(Player player) =>
		$"{player.RunState.CurrentActIndex}:{player.RunState.ActFloor}";

	private static async Task Execute(RitsuLibManagedNetActionContext<PatchworkActionPayload> context)
	{
		Player player = context.Player;
		if (player.Character is not Characters.LaffeyCharacter)
			return;
		if (context.Message.Kind == 1)
		{
			await Purchase(player);
			return;
		}
		if (context.Message.Kind != 0 || player.RunState.CurrentRoom is CombatRoom)
			return;
		PatchworkPlacement placement = new()
		{
			PieceId = context.Message.PieceId,
			X = context.Message.X,
			Y = context.Message.Y,
			Rotation = context.Message.Rotation,
			Flipped = context.Message.Flipped
		};
		PatchworkSaveData state = PatchworkBoard.Get(player);
		List<int> squares = PatchworkBoard.NewSquares(state, placement);
		if (!PatchworkBoard.CanPlace(state, placement))
			return;
		if (squares.Contains(8) && !ValidAncientChoice(player, context.Message.AncientChoice))
			return;
		PatchworkBoard.Modify(player, save =>
		{
			save.AvailablePieces.Remove(placement.PieceId);
			save.Placements.Add(placement);
			save.ClaimedSquares.AddRange(squares);
		});
		foreach (int size in squares)
		{
			if (size == 7)
				await RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(player, RelicRarity.Rare).ToMutable(), player);
			else if (size == 8)
				await GrantAncient(player, context.Message.AncientChoice);
		}
	}

	private static bool ValidAncientChoice(Player player, int choice)
	{
		bool tooth = player.GetRelic<ArchaicTooth>() != null;
		bool touch = player.GetRelic<TouchOfOrobas>() != null;
		return (tooth && touch && choice == 0)
			|| (tooth && !touch && choice == 2)
			|| (!tooth && touch && choice == 1)
			|| (!tooth && !touch && choice is 1 or 2);
	}

	private static async Task GrantAncient(Player player, int choice)
	{
		if (choice == 1 && player.GetRelic<ArchaicTooth>() == null)
		{
			ArchaicTooth tooth = (ArchaicTooth)ModelDb.Relic<ArchaicTooth>().ToMutable();
			tooth.SetupForPlayer(player);
			await RelicCmd.Obtain(tooth, player);
		}
		if (choice == 2 && player.GetRelic<TouchOfOrobas>() == null)
		{
			TouchOfOrobas touch = (TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
			touch.SetupForPlayer(player);
			await RelicCmd.Obtain(touch, player);
		}
	}

	private static async Task Purchase(Player player)
	{
		if (player.RunState.CurrentRoom is not MerchantRoom || player.Gold < 50)
			return;
		string key = ShopKey(player);
		PatchworkSaveData state = PatchworkBoard.Get(player);
		if (state.PurchasedShops.Contains(key) || !PatchworkBoard.HasSpecialStock(state))
			return;
		await PlayerCmd.LoseGold(50, player, GoldLossType.Spent);
		PatchworkBoard.Modify(player, state =>
		{
			state.PurchasedShops.Add(key);
			state.AvailablePieces.Add(0);
		});
	}
}
