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
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Networking.ManagedActions;
using STS2RitsuLib.Screens;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public sealed record PatchworkActionPayload(int Kind, int PieceId, int X, int Y, int Rotation, bool Flipped,
	int PlacementIndex = -1, PatchworkPlacement? OriginalPlacement = null, string? ShopId = null);

public static class PatchworkActions
{
	private static readonly HashSet<Player> ChoosingAncient = [];
	public static bool IsChoosingAncient(Player player) => ChoosingAncient.Contains(player);
	private static readonly RitsuLibManagedNetActionDescriptor<PatchworkActionPayload> Descriptor = new(
		MainFile.ModId,
		"patchwork_place_or_buy",
		payload => JsonSerializer.SerializeToUtf8Bytes(payload),
		bytes => JsonSerializer.Deserialize<PatchworkActionPayload>(bytes)!,
		Execute,
		GameActionType.NonCombat);

	public static void Register() => RitsuLibManagedNetActions.Register(Descriptor);

	public static bool RequestPlacement(Player player, PatchworkPlacement placement) =>
		RitsuLibManagedNetActions.Request(RunManager.Instance, Descriptor,
			new PatchworkActionPayload(0, placement.PieceId, placement.X, placement.Y,
				placement.Rotation, placement.Flipped), player.NetId);

	public static bool RequestShopPurchase(Player player, string shopKey) =>
		RitsuLibManagedNetActions.Request(RunManager.Instance, Descriptor,
			new PatchworkActionPayload(1, 0, 0, 0, 0, false, ShopId: shopKey), player.NetId);

	public static bool RequestMove(Player player, PatchworkPlacement placement, int index, PatchworkPlacement original) =>
		RitsuLibManagedNetActions.Request(RunManager.Instance, Descriptor,
			new PatchworkActionPayload(2, placement.PieceId, placement.X, placement.Y, placement.Rotation,
				placement.Flipped, index, PatchworkBoard.Copy(original)), player.NetId);

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
		if (context.Message.Kind is not (0 or 2) || !PatchworkAccess.CanUseFor(player))
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
		bool reopen = false;
		try
		{
			RelicModel? ancient = null;
			if (squares.Contains(8))
			{
				List<RelicModel> options = AncientOptions(player);
				if (options.Count > 1)
				{
					ChoosingAncient.Add(player);
					if (LocalContext.IsMe(player) && RunManager.Instance.NetService.Type != NetGameType.Replay)
					{
						reopen = ModScreenService.CurrentCapstoneScreen is PatchworkScreen;
						if (reopen) ModScreenService.Close();
						NMapScreen.Instance?.Close(animateOut: false);
					}
					ancient = await RelicSelectCmd.FromChooseARelicScreen(player, options);
					if (ancient == null) return;
				}
				else if (options.Count == 1) ancient = options[0];
			}
			if (!ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), player.RunState) || !PatchworkAccess.CanUse) return;
			bool applied = false;
			PatchworkBoard.Modify(player, save => applied = PatchworkBoard.TryApply(save, placement, out squares,
				movingIndex, context.Message.OriginalPlacement));
			if (!applied) return;
			foreach (int size in squares)
			{
				if (size == 7)
					for (int index = 0; index < PatchworkBalance.RareRelicCount; index++)
						await RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(player, RelicRarity.Rare).ToMutable(), player);
				if (size == 8 && ancient != null) await RelicCmd.Obtain(ancient, player);
			}
		}
		finally
		{
			ChoosingAncient.Remove(player);
			if (reopen && ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), player.RunState) && PatchworkAccess.CanUse &&
				ModScreenService.CurrentCapstoneScreen == null)
				ModScreenService.Open(PatchworkScreen.Create(player));
		}
	}

	private static List<RelicModel> AncientOptions(Player player)
	{
		List<RelicModel> options = [];
		if (player.GetRelic<ArchaicTooth>() == null)
		{
			ArchaicTooth tooth = (ArchaicTooth)ModelDb.Relic<ArchaicTooth>().ToMutable();
			tooth.SetupForPlayer(player); options.Add(tooth);
		}
		if (player.GetRelic<TouchOfOrobas>() == null)
		{
			TouchOfOrobas touch = (TouchOfOrobas)ModelDb.Relic<TouchOfOrobas>().ToMutable();
			touch.SetupForPlayer(player); options.Add(touch);
		}
		return options;
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
