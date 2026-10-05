using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.RunData;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public sealed class PatchworkPlacement
{
	public int PieceId { get; set; }
	public int X { get; set; }
	public int Y { get; set; }
	public int Rotation { get; set; }
	public bool Flipped { get; set; }
}

public sealed class PatchworkSaveData
{
	public List<int> AvailablePieces { get; set; } = [];
	public List<PatchworkPlacement> Placements { get; set; } = [];
	public List<int> ClaimedSquares { get; set; } = [];
	public List<string> PurchasedShops { get; set; } = [];
}

public static class PatchworkBoard
{
	public const int BoardSize = 10;
	public const int FirstRewardSize = 3;
	private static readonly string[][] Shapes =
	[
		["X"],
		["XX"],
		[" X", "XX"], [" X", "XX"], [" X", "XX"],
		["XXX"],
		[" XX", "XX "], [" XX", "XX "],
		["  X", "XXX"], ["  X", "XXX"],
		[" X ", "XXX"], ["X X", "XXX"], [" XX", "XXX"],
		["XXXX"], ["  X ", "XXXX"], [" XX ", "XXXX"],
		[" XXX", "XX  "], ["   X", "XXXX"], ["  XX", "XXXX"],
		[" XXX", "XXX "], ["XXXXX"], [" X ", "XXX", " X "],
		["X  ", "XXX", "X  "], [" XX", "XXX", "X  "],
		[" X ", "XXX", "X X"], ["X X", "XXX", "X X"],
		["  X", " XX", "XX "], [" X  ", "XXXX", " X  "],
		[" XX ", "XXXX", " XX "], ["  X ", "XXXX", " X  "],
		["X   ", "XXXX", "X   "], ["X   ", "XXXX", "   X"],
		["X  X", "XXXX"], ["  X  ", "XXXXX", "  X  "]
	];

	private static PlayerRunSavedData<PatchworkSaveData>? _slot;

	public static void Register()
	{
		_slot = RunSavedDataStore.For(MainFile.ModId).RegisterPerPlayer("patchwork_board", () => new PatchworkSaveData
		{
			AvailablePieces = [0]
		}, new RunSavedDataOptions { SchemaVersion = 1, WritePolicy = RunSavedDataWritePolicy.WhenSet });
	}

	public static PatchworkSaveData Get(Player player) => _slot!.Get(player);

	public static PatchworkSaveData Modify(Player player, Action<PatchworkSaveData> action) => _slot!.Modify(player, action);

	public static bool HasSpecialStock(PatchworkSaveData state) =>
		state.AvailablePieces.Count(id => id == 0) + state.Placements.Count(p => p.PieceId == 0) < PatchworkBalance.ShopMaxChips;

	public static IReadOnlyList<(int X, int Y)> Cells(int pieceId, int rotation, bool flipped)
	{
		if (pieceId < 0 || pieceId >= Shapes.Length)
			return [];
		List<(int X, int Y)> cells = [];
		for (int y = 0; y < Shapes[pieceId].Length; y++)
		for (int x = 0; x < Shapes[pieceId][y].Length; x++)
		{
			if (Shapes[pieceId][y][x] != 'X')
				continue;
			int tx = flipped ? -x : x;
			int ty = y;
			for (int i = 0; i < ((rotation % 4) + 4) % 4; i++)
				(tx, ty) = (-ty, tx);
			cells.Add((tx, ty));
		}
		int minX = cells.Min(cell => cell.X);
		int minY = cells.Min(cell => cell.Y);
		return cells.Select(cell => (cell.X - minX, cell.Y - minY)).ToArray();
	}

	public static bool[,] Occupied(PatchworkSaveData state, int movingIndex = -1)
	{
		bool[,] occupied = new bool[BoardSize, BoardSize];
		foreach (PatchworkPlacement placement in state.Placements.Where((_, index) => index != movingIndex))
		foreach ((int x, int y) in Cells(placement.PieceId, placement.Rotation, placement.Flipped))
		{
			int bx = placement.X + x;
			int by = placement.Y + y;
			if (bx is >= 0 and < BoardSize && by is >= 0 and < BoardSize)
				occupied[bx, by] = true;
		}
		return occupied;
	}

	public static bool CanPlace(PatchworkSaveData state, PatchworkPlacement placement, int movingIndex = -1)
	{
		if (movingIndex < -1 || movingIndex >= state.Placements.Count ||
			(movingIndex < 0 ? !state.AvailablePieces.Contains(placement.PieceId) : state.Placements[movingIndex].PieceId != placement.PieceId) ||
			placement.PieceId < 0 || placement.PieceId >= Shapes.Length ||
			placement.X is < 0 or >= BoardSize || placement.Y is < 0 or >= BoardSize || placement.Rotation is < 0 or > 3)
			return false;
		bool[,] occupied = Occupied(state, movingIndex);
		foreach ((int x, int y) in Cells(placement.PieceId, placement.Rotation, placement.Flipped))
		{
			int bx = placement.X + x;
			int by = placement.Y + y;
			if (bx is < 0 or >= BoardSize || by is < 0 or >= BoardSize || occupied[bx, by])
				return false;
		}
		return true;
	}

	public static List<int> NewSquares(PatchworkSaveData state, PatchworkPlacement placement, int movingIndex = -1)
	{
		if (!CanPlace(state, placement, movingIndex))
			return [];
		bool[,] occupied = Occupied(state, movingIndex);
		foreach ((int x, int y) in Cells(placement.PieceId, placement.Rotation, placement.Flipped))
			occupied[placement.X + x, placement.Y + y] = true;
		int largest = PatchworkGeometry.LargestSquare(occupied, FirstRewardSize)?.Size ?? 0;
		return Enumerable.Range(FirstRewardSize, Math.Max(0, largest - FirstRewardSize + 1))
			.Where(size => !state.ClaimedSquares.Contains(size)).ToList();
	}

	public static PatchworkSquare? LargestCompletedSquare(PatchworkSaveData state) =>
		PatchworkGeometry.LargestSquare(Occupied(state), FirstRewardSize);

	public static PatchworkPlacement Copy(PatchworkPlacement placement) => new()
	{
		PieceId = placement.PieceId, X = placement.X, Y = placement.Y,
		Rotation = placement.Rotation, Flipped = placement.Flipped
	};
	public static bool Matches(PatchworkPlacement a, PatchworkPlacement b) =>
		a.PieceId == b.PieceId && a.X == b.X && a.Y == b.Y && a.Rotation == b.Rotation && a.Flipped == b.Flipped;
	public static bool MatchesOriginal(PatchworkSaveData state, int index, PatchworkPlacement? original) =>
		index >= 0 && index < state.Placements.Count && original != null && Matches(state.Placements[index], original);

	/// <summary>Deterministic commit shared by the network executor and the standalone preview.</summary>
	public static bool TryApply(PatchworkSaveData state, PatchworkPlacement placement, out List<int> rewards,
		int movingIndex = -1, PatchworkPlacement? original = null)
	{
		rewards = [];
		if (!CanPlace(state, placement, movingIndex) ||
			(movingIndex >= 0 && (!MatchesOriginal(state, movingIndex, original) || Matches(placement, original!)))) return false;
		rewards = NewSquares(state, placement, movingIndex);
		if (movingIndex < 0)
		{
			state.AvailablePieces.Remove(placement.PieceId);
			state.Placements.Add(Copy(placement));
		}
		else state.Placements[movingIndex] = Copy(placement);
		state.ClaimedSquares.AddRange(rewards);
		return true;
	}

	public static int RandomUnclaimedPiece(Player player)
	{
		PatchworkSaveData state = Get(player);
		int[] remaining = Enumerable.Range(1, Shapes.Length - 1)
			.Where(id => !state.AvailablePieces.Contains(id) && state.Placements.All(p => p.PieceId != id))
			.ToArray();
		return remaining.Length == 0 ? -1 : remaining[player.PlayerRng.Rewards.NextInt(remaining.Length)];
	}
}
