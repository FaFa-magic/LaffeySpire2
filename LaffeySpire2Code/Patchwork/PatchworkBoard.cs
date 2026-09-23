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
		_slot = RunSavedDataStore.For(MainFile.ModId).RegisterPerPlayer<PatchworkSaveData>("patchwork_board");
	}

	public static PatchworkSaveData Get(Player player) => _slot!.Get(player);

	public static PatchworkSaveData Modify(Player player, Action<PatchworkSaveData> action) => _slot!.Modify(player, action);

	public static bool HasSpecialStock(PatchworkSaveData state) =>
		state.AvailablePieces.Count(id => id == 0) + state.Placements.Count(p => p.PieceId == 0) < 5;

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

	public static bool[,] Occupied(PatchworkSaveData state)
	{
		bool[,] occupied = new bool[10, 10];
		foreach (PatchworkPlacement placement in state.Placements)
		foreach ((int x, int y) in Cells(placement.PieceId, placement.Rotation, placement.Flipped))
		{
			int bx = placement.X + x;
			int by = placement.Y + y;
			if (bx is >= 0 and < 10 && by is >= 0 and < 10)
				occupied[bx, by] = true;
		}
		return occupied;
	}

	public static bool CanPlace(PatchworkSaveData state, PatchworkPlacement placement)
	{
		if (!state.AvailablePieces.Contains(placement.PieceId) || placement.PieceId < 0 || placement.PieceId >= Shapes.Length ||
			placement.X is < 0 or >= 10 || placement.Y is < 0 or >= 10 || placement.Rotation is < 0 or > 3)
			return false;
		bool[,] occupied = Occupied(state);
		foreach ((int x, int y) in Cells(placement.PieceId, placement.Rotation, placement.Flipped))
		{
			int bx = placement.X + x;
			int by = placement.Y + y;
			if (bx is < 0 or >= 10 || by is < 0 or >= 10 || occupied[bx, by])
				return false;
		}
		return true;
	}

	public static List<int> NewSquares(PatchworkSaveData state, PatchworkPlacement placement)
	{
		if (!CanPlace(state, placement))
			return [];
		bool[,] occupied = Occupied(state);
		foreach ((int x, int y) in Cells(placement.PieceId, placement.Rotation, placement.Flipped))
			occupied[placement.X + x, placement.Y + y] = true;
		List<int> completed = [];
		for (int size = 3; size <= 10; size++)
		{
			if (state.ClaimedSquares.Contains(size))
				continue;
			bool found = false;
			for (int y = 0; y <= 10 - size && !found; y++)
			for (int x = 0; x <= 10 - size && !found; x++)
			{
				found = true;
				for (int dy = 0; dy < size && found; dy++)
				for (int dx = 0; dx < size; dx++)
					if (!occupied[x + dx, y + dy])
					{
						found = false;
						break;
					}
			}
			if (found)
				completed.Add(size);
		}
		return completed;
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
