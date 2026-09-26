namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

/// <summary>A display witness, derived from placements; never persisted as reward state.</summary>
public readonly record struct PatchworkSquare(int X, int Y, int Size);

public static class PatchworkGeometry
{
	// Stable tie break: topmost, then leftmost. Accepts arbitrary board dimensions.
	public static PatchworkSquare? LargestSquare(bool[,] occupied, int minimumSize = 3)
	{
		int width = occupied.GetLength(0), height = occupied.GetLength(1);
		int[,] sizes = new int[width + 1, height + 1];
		PatchworkSquare? best = null;
		for (int y = 0; y < height; y++)
		for (int x = 0; x < width; x++)
		{
			if (!occupied[x, y]) continue;
			int size = sizes[x + 1, y + 1] = 1 + Math.Min(sizes[x, y], Math.Min(sizes[x, y + 1], sizes[x + 1, y]));
			if (size >= minimumSize && (best == null || size > best.Value.Size))
				best = new PatchworkSquare(x - size + 1, y - size + 1, size);
		}
		return best;
	}
}
