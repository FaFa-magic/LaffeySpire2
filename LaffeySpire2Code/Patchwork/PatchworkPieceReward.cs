using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.TestSupport;
using STS2RitsuLib.Combat.Rewards;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public sealed class PatchworkPieceReward(Player player, int pieceId) : ModCustomReward(player)
{
	private static RewardType? _rewardType;

	public int PieceId { get; } = pieceId;

	public override RewardType ModRewardType => _rewardType
		?? throw new InvalidOperationException("Patchwork reward has not been registered.");

	public override LocString Description
	{
		get
		{
			LocString description = new("gameplay_ui", "LAFFEY_SPIRE2_REWARD_PATCHWORK_PIECE");
			description.Add("Piece", PieceId);
			return description;
		}
	}

	internal static void Register()
	{
		_rewardType = ModRewardRegistry.For(MainFile.ModId)
			.RegisterOwned("patchwork_piece", (_, player, json) =>
				new PatchworkPieceReward(player, JsonSerializer.Deserialize<int>(json ?? "-1")))
			.RewardType;
	}

	public override string? ToModRewardJson() => JsonSerializer.Serialize(PieceId);

	public override Control? CreateIcon()
	{
		if (TestMode.IsOn || PieceId < 1 || PieceId > 33)
			return null;
		IReadOnlyList<(int X, int Y)> cells = PatchworkBoard.Cells(PieceId, 0, false);
		HashSet<(int, int)> filled = cells.ToHashSet();
		int width = cells.Max(cell => cell.X) + 1;
		int height = cells.Max(cell => cell.Y) + 1;
		Label icon = new()
		{
			Text = string.Join("\n", Enumerable.Range(0, height).Select(y =>
				string.Concat(Enumerable.Range(0, width).Select(x => filled.Contains((x, y)) ? "■" : " ")))),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		icon.AddThemeFontSizeOverride("font_size", 13);
		return icon;
	}

	protected override Task<bool> OnSelect()
	{
		if (PieceId < 1 || PieceId > 33)
			return Task.FromResult(false);
		PatchworkBoard.Modify(Player, state =>
		{
			if (!state.AvailablePieces.Contains(PieceId) &&
				state.Placements.All(placement => placement.PieceId != PieceId))
				state.AvailablePieces.Add(PieceId);
		});
		return Task.FromResult(true);
	}

	public override void MarkContentAsSeen()
	{
	}
}
