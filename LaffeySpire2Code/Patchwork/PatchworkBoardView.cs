using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public partial class PatchworkBoardView : Control
{
	public event Action<int, int>? AnchorChanged;
	public event Action<int, int>? PointerPressed;
	private PatchworkSaveData _state = new();
	private PatchworkPlacement _preview = new() { PieceId = -1 };
	private bool _valid;
	private int _movingIndex = -1;
	private bool _showPreview = true;
	private PatchworkSquare? _square, _prospective;
	private HashSet<int> _newRewards = [];
	private float _pulse;
	private Texture2D _socket = null!;
	private Image _wrenchCursor = null!;
	private const float WrenchCursorSize = 48f * 1.7f;
	private static readonly Vector2 WrenchHotspot = new Vector2(8, 3) * 1.7f;
	private NCursorManager? _cursorManager;
	private bool _cursorOverridden;
	private Vector2? _cursorPointer;
	public float GridPadding => Math.Min(Size.X, Size.Y) * 0.10f;
	public float CellStep => (Math.Min(Size.X, Size.Y) - GridPadding * 2) / PatchworkBoard.BoardSize;
	private Rect2 GridRect => new(Vector2.One * GridPadding, Vector2.One * (CellStep * PatchworkBoard.BoardSize));
	private Rect2 Cell(int x, int y) => new(new Vector2(GridPadding + x * CellStep, GridPadding + y * CellStep), Vector2.One * CellStep);
	public Vector2 RewardPort(int size) => new(Size.X * 0.012f,
		GridPadding + (size - PatchworkBoard.FirstRewardSize + 0.5f) * (Size.Y - GridPadding * 2) / 8);
	public Vector2I PointerCell(Vector2 globalPosition)
	{
		Vector2 local = GetGlobalTransformWithCanvas().AffineInverse() * globalPosition;
		return new Vector2I((int)Math.Floor((local.X - GridPadding) / CellStep), (int)Math.Floor((local.Y - GridPadding) / CellStep));
	}
	public static bool IsBoardCell(Vector2I cell) => cell.X is >= 0 and < PatchworkBoard.BoardSize && cell.Y is >= 0 and < PatchworkBoard.BoardSize;
	public void Present(PatchworkSaveData state, PatchworkPlacement preview, int movingIndex = -1, bool showPreview = true)
	{
		_state = state; _preview = preview; _movingIndex = movingIndex; _showPreview = showPreview;
		_valid = PatchworkBoard.CanPlace(state, preview, movingIndex);
		_square = PatchworkBoard.LargestCompletedSquare(state);
		_newRewards = _valid && showPreview ? PatchworkBoard.NewSquares(state, preview, movingIndex).ToHashSet() : [];
		_prospective = null;
		if (_newRewards.Count > 0)
		{
			bool[,] occupied = PatchworkBoard.Occupied(state, movingIndex);
			foreach (var c in PatchworkBoard.Cells(preview.PieceId, preview.Rotation, preview.Flipped)) occupied[preview.X + c.X, preview.Y + c.Y] = true;
			_prospective = PatchworkGeometry.LargestSquare(occupied);
		}
		QueueRedraw();
		UpdateCursor();
	}
	public override void _Ready()
	{
		_socket = ResourceLoader.Load<Texture2D>(PatchworkVisuals.AssetRoot + "chip-socket-v2.png");
		FocusMode = FocusModeEnum.All;
		MouseDefaultCursorShape = CursorShape.Arrow;
		_wrenchCursor = ResourceLoader.Load<Texture2D>(PatchworkVisuals.AssetRoot + "cursor_wrench.png").GetImage();
		float cursorScale = WrenchCursorSize / Math.Max(_wrenchCursor.GetWidth(), _wrenchCursor.GetHeight());
		_wrenchCursor.Resize(Math.Max(1, (int)Math.Round(_wrenchCursor.GetWidth() * cursorScale)),
			Math.Max(1, (int)Math.Round(_wrenchCursor.GetHeight() * cursorScale)), Image.Interpolation.Lanczos);
		TextureFilter = TextureFilterEnum.Linear;
		Resized += QueueRedraw;
	}
	public override void _Process(double delta)
	{
		_pulse += (float)delta;
		UpdateCursor();
		if (_square != null || _newRewards.Count > 0) QueueRedraw();
	}
	private void UpdateCursor()
	{
		if (_wrenchCursor == null) return;
		bool useWrench = _preview.PieceId >= 0 && IsVisibleInTree() &&
			IsBoardCell(PointerCell(_cursorPointer ?? GetGlobalMousePosition())) && GetViewport().GuiGetHoveredControl() == this;
		if (!useWrench) { RestoreCursor(); return; }
		if (_cursorOverridden) return;
		_cursorManager = NGame.Instance?.CursorManager;
		if (_cursorManager != null) _cursorManager.OverrideCursor(_wrenchCursor, _wrenchCursor, WrenchHotspot);
		else Input.SetCustomMouseCursor(_wrenchCursor, Input.CursorShape.Arrow, WrenchHotspot);
		_cursorOverridden = true;
	}
	private void RestoreCursor()
	{
		if (!_cursorOverridden) return;
		if (_cursorManager != null && GodotObject.IsInstanceValid(_cursorManager)) _cursorManager.StopOverridingCursor();
		else Input.SetCustomMouseCursor(null, Input.CursorShape.Arrow);
		_cursorOverridden = false; _cursorManager = null;
	}
	public override void _ExitTree()
	{
		RestoreCursor();
		_wrenchCursor?.Dispose();
	}
	public override void _Input(InputEvent input)
	{
		if (input is InputEventMouseMotion motion) _cursorPointer = motion.Position;
		else if (input is InputEventMouseButton button) _cursorPointer = button.Position;
	}
	public override void _GuiInput(InputEvent input)
	{
		if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse)
		{
			int x = (int)Math.Floor((mouse.Position.X - GridPadding) / CellStep);
			int y = (int)Math.Floor((mouse.Position.Y - GridPadding) / CellStep);
			if (x >= 0 && x < PatchworkBoard.BoardSize && y >= 0 && y < PatchworkBoard.BoardSize)
			{ GrabFocus(); PointerPressed?.Invoke(x, y); AcceptEvent(); }
		}
		if (input is InputEventKey { Pressed: true } key)
		{
			var offset = key.Keycode switch { Key.Left => new Vector2I(-1, 0), Key.Right => new Vector2I(1, 0),
				Key.Up => new Vector2I(0, -1), Key.Down => new Vector2I(0, 1), _ => Vector2I.Zero };
			if (offset != Vector2I.Zero)
			{ AnchorChanged?.Invoke(Math.Clamp(_preview.X + offset.X, 0, PatchworkBoard.BoardSize - 1),
				Math.Clamp(_preview.Y + offset.Y, 0, PatchworkBoard.BoardSize - 1)); AcceptEvent(); }
		}
	}
	public override void _Draw()
	{
		DrawTextureRect(_socket, new Rect2(Vector2.Zero, Size), false);
		for (int y = 0; y < PatchworkBoard.BoardSize; y++)
		for (int x = 0; x < PatchworkBoard.BoardSize; x++)
		{
			Rect2 rect = Cell(x, y);
			DrawRect(rect, new Color("0e2536"));
			Vector2 center = rect.GetCenter();
			DrawCircle(center, 1.4f, new Color("937f58"));
			DrawLine(center - new Vector2(5, 0), center + new Vector2(5, 0), new Color("49606b"));
			DrawLine(center - new Vector2(0, 5), center + new Vector2(0, 5), new Color("49606b"));
		}
		// Paint every grid boundary after all cell fills; adjacent fills must not erase half of a line.
		for (int line = 0; line <= PatchworkBoard.BoardSize; line++)
		{
			float coordinate = GridPadding + line * CellStep;
			float end = GridPadding + PatchworkBoard.BoardSize * CellStep;
			Color grid = new("385569");
			DrawLine(new Vector2(coordinate, GridPadding), new Vector2(coordinate, end), grid, 1.25f, true);
			DrawLine(new Vector2(GridPadding, coordinate), new Vector2(end, coordinate), grid, 1.25f, true);
		}
		for (int size = 3; size <= 10; size++)
		{
			bool active = _state.ClaimedSquares.Contains(size), preview = _newRewards.Contains(size);
			PatchworkSquare? witness = preview ? _prospective : _square;
			if (witness is not { } square || (!active && !preview)) continue;
			Color color = preview ? PatchworkVisuals.Amber : PatchworkVisuals.Cyan;
			Vector2 port = RewardPort(size);
			Vector2 source = Cell(square.X, square.Y).Position + new Vector2(0, Math.Min(size, square.Size) * CellStep / 2);
			float busX = GridPadding * (0.3f + (size - 3) * 0.035f);
			Vector2[] points = [source, new(busX, source.Y), new(busX, port.Y), port];
			DrawPolyline(points, new Color(color, 0.17f), 7, true);
			DrawPolyline(points, new Color(color, 0.85f), 1.5f, true);
		}
		for (int index = 0; index < _state.Placements.Count; index++)
			if (index != _movingIndex || !_showPreview) DrawPiece(_state.Placements[index], false);
		if (_preview.PieceId >= 0 && _showPreview) DrawPiece(_preview, true);
		if (_square is { } completed) DrawSquare(completed, PatchworkVisuals.Cyan, true);
		if (_prospective is { } predicted) DrawSquare(predicted, PatchworkVisuals.Amber, false);
		for (int size = 3; size <= 10; size++)
		{
			Color color = _state.ClaimedSquares.Contains(size) ? PatchworkVisuals.Cyan : _newRewards.Contains(size) ? PatchworkVisuals.Amber : new Color("5c6270");
			Vector2 port = RewardPort(size);
			DrawCircle(port, 5, new Color("0a1118"));
			DrawCircle(port, 2.8f, color);
		}
	}
	private void DrawSquare(PatchworkSquare square, Color color, bool glow)
	{
		Rect2 outline = new(Cell(square.X, square.Y).Position, Vector2.One * (square.Size * CellStep));
		float pulse = 0.8f + 0.2f * MathF.Sin(_pulse * 2.5f);
		if (glow) for (int i = 7; i >= 1; i--) DrawRect(outline.Grow(i), new Color(color, (8 - i) * 0.025f * pulse), false, 2);
		DrawRect(outline, new Color(color, pulse), false, glow ? 2.5f : 1.5f);
		foreach (Vector2 corner in new[] { outline.Position, outline.Position + new Vector2(outline.Size.X, 0), outline.End,
			outline.Position + new Vector2(0, outline.Size.Y) }) DrawCircle(corner, 3, color.Lightened(0.5f));
	}
	private void DrawPiece(PatchworkPlacement placement, bool ghost)
	{
		Color? outline = ghost ? (_valid ? PatchworkVisuals.Cyan : PatchworkVisuals.Red) : null;
		PatchworkVisuals.DrawChip(this, placement.PieceId, placement.Rotation, placement.Flipped, Cell(placement.X, placement.Y).Position,
			CellStep, new Color(1, 1, 1, ghost ? 0.52f : 1), outline, GridRect);
	}
}
