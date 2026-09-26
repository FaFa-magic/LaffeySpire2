using Godot;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public partial class PatchworkPieceView : Control
{
	private int _pieceId = -1, _rotation;
	private bool _flipped;
	public void ShowPiece(int id, int rotation = 0, bool flipped = false)
	{
		_pieceId = id; _rotation = rotation; _flipped = flipped; QueueRedraw();
	}
	public override void _Draw()
	{
		if (_pieceId < 0) return;
		var cells = PatchworkBoard.Cells(_pieceId, _rotation, _flipped);
		if (cells.Count == 0) return;
		int width = cells.Max(c => c.X) + 1, height = cells.Max(c => c.Y) + 1;
		float step = Math.Min(76, Math.Min((Size.X - 6) / width, (Size.Y - 6) / height));
		Vector2 origin = (Size - new Vector2(width, height) * step) / 2;
		PatchworkVisuals.DrawChip(this, _pieceId, _rotation, _flipped, origin, step, Colors.White);
	}
}
