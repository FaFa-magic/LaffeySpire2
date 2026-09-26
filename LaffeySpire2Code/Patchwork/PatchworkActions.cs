using System.Text.Json;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Networking.ManagedActions;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public sealed record PatchworkActionPayload(int Kind, int PieceId, int X, int Y, int Rotation, bool Flipped, int AncientChoice,
	int PlacementIndex = -1, PatchworkPlacement? OriginalPlacement = null, string? ShopId = null);

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

	public static bool RequestShopPurchase(Player player, string shopKey) =>
		RitsuLibManagedNetActions.Request(RunManager.Instance, Descriptor,
			new PatchworkActionPayload(1, 0, 0, 0, 0, false, 0, ShopId: shopKey), player.NetId);

	public static bool RequestMove(Player player, PatchworkPlacement placement, int index, PatchworkPlacement original, int ancientChoice) =>
		RitsuLibManagedNetActions.Request(RunManager.Instance, Descriptor,
			new PatchworkActionPayload(2, placement.PieceId, placement.X, placement.Y, placement.Rotation,
				placement.Flipped, ancientChoice, index, PatchworkBoard.Copy(original)), player.NetId);

	public static string ShopKey(Player player) =>
		$"{player.RunState.CurrentActIndex}:{player.RunState.ActFloor}";

	private static async Task Execute(RitsuLibManagedNetActionContext<PatchworkActionPayload> context)
	{
		Player player = context.Player;
		if (player.Character is not Characters.LaffeyCharacter)
			return;
		if (context.Message.Kind == 1)
		{
			await Purchase(player, context.Message.ShopId);
			return;
		}
		if (context.Message.Kind is not (0 or 2) || !PatchworkAccess.CanUse)
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
		int movingIndex = context.Message.Kind == 2 ? context.Message.PlacementIndex : -1;
		if (context.Message.Kind == 2 && !PatchworkBoard.MatchesOriginal(state, movingIndex, context.Message.OriginalPlacement))
			return;
		List<int> squares = PatchworkBoard.NewSquares(state, placement, movingIndex);
		if (!PatchworkBoard.CanPlace(state, placement, movingIndex))
			return;
		if (squares.Contains(8) && !ValidAncientChoice(player, context.Message.AncientChoice))
			return;
		bool applied = false;
		PatchworkBoard.Modify(player, save => applied = PatchworkBoard.TryApply(save, placement, out squares,
			movingIndex, context.Message.OriginalPlacement));
		if (!applied) return;
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

	private static async Task Purchase(Player player, string? shopId)
	{
		if (player.RunState.CurrentRoom is not MerchantRoom room || shopId != ShopKey(player))
			return;
		var inventory = room.Inventories.FirstOrDefault(i => i.Player == player);
		if (inventory == null || PatchworkShop.GetEntry(inventory) is not { IsStocked: true } entry)
			return;
		var synchronizer = RunManager.Instance.PlayerChoiceSynchronizer;
		uint choiceId = synchronizer.ReserveChoiceId(player);
		int price;
		if (LocalContext.IsMe(player) && RunManager.Instance.NetService.Type != NetGameType.Replay)
		{
			price = Math.Max(0, entry.Cost);
			synchronizer.SyncLocalChoice(player, choiceId, PlayerChoiceResult.FromIndex(price));
		}
		else price = (await synchronizer.WaitForRemoteChoice(player, choiceId)).AsIndex();
		await entry.PurchaseSynchronized(inventory, price);
	}
}
