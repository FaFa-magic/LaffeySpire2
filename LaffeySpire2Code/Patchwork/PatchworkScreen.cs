using Godot;
using LaffeySpire2.LaffeySpire2Code.Characters;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Screens;
using STS2RitsuLib.TopBar;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

[RegisterOwnedTopBarButton("patchwork", ButtonOrder = 0, IconPath = PatchworkVisuals.AssetRoot + "chips/chip_00.png")]
public sealed class PatchworkTopBarButton : IModTopBarButtonHandler
{
	public void OnClick(ModTopBarButtonContext context)
	{
		if (context.Player is not { Character: LaffeyCharacter } player || !PatchworkAccess.CanUse)
			return;
		if (ModScreenService.CurrentCapstoneScreen is PatchworkScreen)
			context.CloseCapstoneScreen();
		else
			context.OpenCapstoneScreen(PatchworkScreen.Create(player));
	}

	public bool IsVisible(ModTopBarButtonContext context) =>
		context.Player is { Character: LaffeyCharacter } player &&
		PatchworkAccess.CanUse;

	public bool IsOpen(ModTopBarButtonContext context) =>
		ModScreenService.CurrentCapstoneScreen is PatchworkScreen;

	public int GetCount(ModTopBarButtonContext context) => context.Player is { Character: LaffeyCharacter } player
		? PatchworkBoard.Get(player).AvailablePieces.Count
		: -1;
}

public sealed partial class PatchworkScreen : Control, ICapstoneScreen
{
	public const string ScenePath = "res://LaffeySpire2/scenes/ui/patchwork_screen.tscn";
	[Export] public bool PreviewMode { get; set; }
	[Export] public string PreviewLanguage { get; set; } = "zhs";
	private Player _player = null!;
	private PatchworkSaveData _previewState = new();
	private Dictionary<string, string> _previewText = [];
	private PatchworkSaveData BoardState => PreviewMode ? _previewState : PatchworkBoard.Get(_player);
	private string Text(string key, params (string Name, object Value)[] values)
	{
		if (!PreviewMode) return PatchworkVisuals.Text(key, values);
		string text = _previewText.GetValueOrDefault("LAFFEY_PATCHWORK_" + key, key);
		foreach (var value in values) text = text.Replace("{" + value.Name + "}", value.Value.ToString());
		return text;
	}
	private string PieceName(int id) => Text(id == 0 ? "SHOP_MODULE" : "MODULE",
		("Type", Text("CHIP_" + id.ToString("D2"))), ("Id", id), ("Cells", PatchworkBoard.Cells(id, 0, false).Count));
	private Control _workspace = null!;
	private PatchworkBoardView _board = null!;
	private PatchworkPieceView _shape = null!;
	private PatchworkPieceView _dragGhost = null!;
	private PatchworkWiringView _wiring = null!;
	private VBoxContainer _pieces = null!;
	private VBoxContainer _rewards = null!;
	private Label _status = null!, _summary = null!, _selected = null!, _inventory = null!;
	private Button _confirm = null!, _close = null!;
	private OptionButton _ancientChoice = null!;
	private readonly Dictionary<int, (PanelContainer Row, Panel Led, Label State)> _rewardRows = [];
	private int _selectedPiece = -1, _rotation, _x, _y;
	private bool _flipped, _pending;
	private int _movingIndex = -1;
	private PatchworkPlacement? _originalPlacement;
	private bool _dragging, _dragStarted, _pointerOnBoard;
	private Vector2 _dragStart;
	private Vector2I _grabCell, _dragStartAnchor;
	private double _pollSeconds, _pendingSeconds;
	private string _lastState = "";

	public static PatchworkScreen Create(Player player)
	{
		PatchworkScreen screen = ResourceLoader.Load<PackedScene>(ScenePath).Instantiate<PatchworkScreen>();
		screen._player = player;
		return screen;
	}
	public NetScreenType ScreenType => NetScreenType.CardPile;
	public bool UseSharedBackstop => true;
	public Control? DefaultFocusedControl => _close;

	public override void _Ready()
	{
		if (PreviewMode)
		{
			string language = PreviewLanguage == "eng" ? "eng" : "zhs";
			_previewText = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
				Godot.FileAccess.GetFileAsString($"res://LaffeySpire2/localization/{language}/gameplay_ui.json"))!;
			_previewState = new PatchworkSaveData
			{
				AvailablePieces = [2, 6, 10, 21, 28, 0], ClaimedSquares = [3, 4],
				Placements = [new() { PieceId = 13 }, new() { PieceId = 5, Y = 1 }, new() { PieceId = 0, X = 3, Y = 1 },
					new() { PieceId = 1, Y = 2 }, new() { PieceId = 1, X = 2, Y = 2 }, new() { PieceId = 13, Y = 3 },
					new() { PieceId = 0, X = 4 }, new() { PieceId = 0, X = 4, Y = 1 }, new() { PieceId = 0, X = 4, Y = 2 },
					new() { PieceId = 5, Y = 4 },
					new() { PieceId = 21, X = 6, Y = 3 }, new() { PieceId = 28, X = 4, Y = 7 }]
			};
		}
		_workspace = GetNode<Control>("Workspace");
		_board = _workspace.GetNode<PatchworkBoardView>("Board");
		_wiring = _workspace.GetNode<PatchworkWiringView>("Wiring");
		_shape = _workspace.GetNode<PatchworkPieceView>("InventoryPanel/Shape");
		_dragGhost = new PatchworkPieceView { MouseFilter = MouseFilterEnum.Ignore, Visible = false, ZIndex = 20 };
		_workspace.AddChild(_dragGhost);
		_shape.MouseFilter = MouseFilterEnum.Stop;
		_shape.GuiInput += input =>
		{
			if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse && _selectedPiece >= 0 && !_pending)
				BeginDrag(GetViewport().GetMousePosition(), new Vector2I(PatchworkBoard.Cells(_selectedPiece, _rotation, _flipped)[0].X,
					PatchworkBoard.Cells(_selectedPiece, _rotation, _flipped)[0].Y));
		};
		_pieces = _workspace.GetNode<VBoxContainer>("InventoryPanel/Scroll/Pieces");
		_rewards = _workspace.GetNode<VBoxContainer>("RewardsPanel/Rows");
		_status = _workspace.GetNode<Label>("Status");
		_summary = _workspace.GetNode<Label>("BoardSummary");
		_selected = _workspace.GetNode<Label>("InventoryPanel/Selected");
		_inventory = _workspace.GetNode<Label>("InventoryPanel/Heading");
		_confirm = _workspace.GetNode<Button>("Confirm");
		_close = _workspace.GetNode<Button>("Close");
		_ancientChoice = _workspace.GetNode<OptionButton>("InventoryPanel/AncientChoice");
		Localize(_workspace);
		foreach (string path in new[] { "InventoryPanel", "RewardsPanel", "ReadoutPanel" })
			_workspace.GetNode<Panel>(path).AddThemeStyleboxOverride("panel",
				PatchworkVisuals.Panel(new Color(0.026f, 0.053f, 0.081f, 0.92f), new Color("536777"), 5));
		foreach (string path in new[] { "Confirm", "Close", "InventoryPanel/Rotate", "InventoryPanel/Flip" })
			PatchworkVisuals.StyleButton(_workspace.GetNode<Button>(path));
		PatchworkVisuals.StyleButton(_ancientChoice);
		_board.AnchorChanged += (x, y) => { _x = x; _y = y; Refresh(); };
		_board.PointerPressed += BoardPointerPressed;
		_workspace.GetNode<Button>("InventoryPanel/Rotate").Pressed += () => { _rotation = (_rotation + 1) % 4; Refresh(); };
		_workspace.GetNode<Button>("InventoryPanel/Flip").Pressed += () => { _flipped = !_flipped; Refresh(); };
		_ancientChoice.AddItem(PreviewMode ? "Archaic Tooth" : MegaCrit.Sts2.Core.Models.ModelDb.Relic<ArchaicTooth>().Title.GetFormattedText(), 1);
		_ancientChoice.AddItem(PreviewMode ? "Touch of Orobas" : MegaCrit.Sts2.Core.Models.ModelDb.Relic<TouchOfOrobas>().Title.GetFormattedText(), 2);
		_ancientChoice.Select(0);
		_confirm.Pressed += Confirm;
		_close.Pressed += () => { if (PreviewMode) GetTree().Quit(); else ModScreenService.Close(); };
		CreateRewardRows();
		_wiring.Configure(_board, _rewardRows.ToDictionary(row => row.Key, row => row.Value.Led));
		Resized += FitWorkspace;
		FitWorkspace();
		Refresh();
	}
	private void Localize(Node node)
	{
		if (node.HasMeta("loc"))
		{
			string text = Text(node.GetMeta("loc").AsString());
			if (node is Label label) label.Text = text;
			if (node is Button button) button.Text = text;
		}
		foreach (Node child in node.GetChildren()) Localize(child);
	}
	private void FitWorkspace()
	{
		float scale = Math.Min(1.18f, Math.Min((Size.X - 24) / 1680f, (Size.Y - 24) / 1000f));
		scale = Math.Max(0.1f, scale);
		_workspace.Scale = Vector2.One * scale;
		_workspace.Position = (Size - new Vector2(1680, 1000) * scale) / 2;
	}
	private void CreateRewardRows()
	{
		for (int size = PatchworkBoard.FirstRewardSize; size <= PatchworkBoard.BoardSize; size++)
		{
			PanelContainer row = new() { CustomMinimumSize = new Vector2(0, 56), MouseFilter = MouseFilterEnum.Ignore };
			_rewards.AddChild(row);
			HBoxContainer content = new(); content.AddThemeConstantOverride("separation", 10); row.AddChild(content);
			Panel led = new() { CustomMinimumSize = new Vector2(10, 10), SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
			Label dimensions = new() { Text = $"{size}×{size}", CustomMinimumSize = new Vector2(44, 0), VerticalAlignment = VerticalAlignment.Center };
			dimensions.AddThemeFontSizeOverride("font_size", 16); content.AddChild(dimensions);
			VBoxContainer description = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill }; content.AddChild(description);
			description.AddThemeConstantOverride("separation", 2);
			Label effect = new() { Text = Text("REWARD_" + size), AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
			effect.AddThemeFontSizeOverride("font_size", 14); description.AddChild(effect);
			Label state = new(); state.AddThemeFontSizeOverride("font_size", 11); description.AddChild(state);
			content.AddChild(led);
			_rewardRows[size] = (row, led, state);
		}
	}
	private static string StateKey(PatchworkSaveData state) =>
		string.Join(",", state.AvailablePieces) + ";" + string.Join(",", state.ClaimedSquares) + ";" +
		string.Join(";", state.Placements.Select(p => $"{p.PieceId},{p.X},{p.Y},{p.Rotation},{p.Flipped}"));

	private void SelectInventory(int id)
	{
		_selectedPiece = id; _movingIndex = -1; _originalPlacement = null; _rotation = 0; _flipped = false;
		Refresh();
	}
	private void BoardPointerPressed(int x, int y)
	{
		if (_pending) return;
		Vector2I grab = Vector2I.Zero;
		if (_selectedPiece >= 0)
		{
			if (PatchworkBoard.Cells(_selectedPiece, _rotation, _flipped).Contains((x - _x, y - _y))) grab = new Vector2I(x - _x, y - _y);
			else { _x = x; _y = y; }
		}
		else
		{
			var placements = BoardState.Placements;
			int index = placements.FindIndex(p => PatchworkBoard.Cells(p.PieceId, p.Rotation, p.Flipped).Contains((x - p.X, y - p.Y)));
			if (index < 0) return;
			PatchworkPlacement original = placements[index];
			_movingIndex = index; _originalPlacement = PatchworkBoard.Copy(original); _selectedPiece = original.PieceId;
			_x = original.X; _y = original.Y; _rotation = original.Rotation; _flipped = original.Flipped;
			grab = new Vector2I(x - _x, y - _y);
		}
		BeginDrag(GetViewport().GetMousePosition(), grab);
	}
	private void BeginDrag(Vector2 pointer, Vector2I grab)
	{
		_dragging = true; _dragStarted = false; _dragStart = pointer; _grabCell = grab;
		_dragStartAnchor = new Vector2I(_x, _y); _pointerOnBoard = PatchworkBoardView.IsBoardCell(_board.PointerCell(pointer));
		Refresh();
	}
	public override void _Input(InputEvent input)
	{
		if (!_dragging) return;
		if (input is InputEventKey { Pressed: true, Keycode: Key.Escape })
		{
			EndDrag(false); GetViewport().SetInputAsHandled(); return;
		}
		if (input is InputEventMouseMotion motion)
		{
			if (!_dragStarted && motion.Position.DistanceTo(_dragStart) < 5) return;
			bool justStarted = !_dragStarted; _dragStarted = true;
			Vector2I cell = _board.PointerCell(motion.Position);
			bool over = PatchworkBoardView.IsBoardCell(cell);
			bool changed = justStarted || over != _pointerOnBoard;
			_pointerOnBoard = over;
			if (over)
			{
				Vector2I anchor = cell - _grabCell;
				changed |= _x != anchor.X || _y != anchor.Y; _x = anchor.X; _y = anchor.Y;
			}
			var cells = PatchworkBoard.Cells(_selectedPiece, _rotation, _flipped);
			_dragGhost.Size = new Vector2(cells.Max(c => c.X) + 1, cells.Max(c => c.Y) + 1) * _board.CellStep + Vector2.One * 6;
			_dragGhost.Position = _workspace.GetGlobalTransformWithCanvas().AffineInverse() * motion.Position -
				(new Vector2(_grabCell.X, _grabCell.Y) + Vector2.One / 2) * _board.CellStep - Vector2.One * 3;
			_dragGhost.ShowPiece(_selectedPiece, _rotation, _flipped); _dragGhost.Visible = !over;
			if (changed) Refresh();
			GetViewport().SetInputAsHandled();
		}
		if (input is InputEventMouseButton { Pressed: false, ButtonIndex: MouseButton.Left })
		{
			bool dragged = _dragStarted;
			EndDrag(_pointerOnBoard || !dragged);
			if (dragged) GetViewport().SetInputAsHandled();
		}
	}
	private void EndDrag(bool keepPosition)
	{
		if (!keepPosition) { _x = _dragStartAnchor.X; _y = _dragStartAnchor.Y; }
		_dragging = false; _dragStarted = false; _dragGhost.Visible = false;
		Refresh();
	}
	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusOut && _dragging) EndDrag(false);
	}
	public override void _Process(double delta)
	{
		if (_board == null) return;
		if (!PreviewMode && !PatchworkAccess.CanUse) { ModScreenService.Close(); return; }
		if (_pending)
		{
			_pendingSeconds += delta;
			if (_pendingSeconds > 12) { _pending = false; Refresh(); _status.Text = Text("SYNC_DELAY"); }
		}
		_pollSeconds += delta;
		if (_pollSeconds < 0.15) return;
		_pollSeconds = 0;
		string state = StateKey(BoardState);
		if (state == _lastState) return;
		_pending = false;
		Refresh();
	}
	private void Refresh()
	{
		if (_board == null) return;
		PatchworkSaveData state = BoardState;
		_lastState = StateKey(state);
		if (_movingIndex >= 0 && !PatchworkBoard.MatchesOriginal(state, _movingIndex, _originalPlacement))
		{ _selectedPiece = -1; _movingIndex = -1; _originalPlacement = null; _dragging = false; _dragGhost.Visible = false; }
		if (_movingIndex < 0 && !state.AvailablePieces.Contains(_selectedPiece))
		{ _selectedPiece = -1; _dragging = false; _dragStarted = false; _dragGhost.Visible = false; }
		PatchworkPlacement placement = CurrentPlacement();
		bool valid = _selectedPiece >= 0 && PatchworkBoard.CanPlace(state, placement, _movingIndex);
		_board.Present(state, placement, _movingIndex, !(_dragging && _dragStarted && !_pointerOnBoard));
		_shape.ShowPiece(_selectedPiece, _rotation, _flipped);
		// Only rebuild the inventory when its data changes, keeping keyboard focus intact.
		string inventoryKey = string.Join(",", state.AvailablePieces);
		if (!_pieces.HasMeta("state") || _pieces.GetMeta("state").AsString() != inventoryKey)
		{
			foreach (Node child in _pieces.GetChildren()) { _pieces.RemoveChild(child); child.QueueFree(); }
			_pieces.SetMeta("state", inventoryKey);
			foreach (int id in state.AvailablePieces)
			{
				int pieceId = id;
				Button button = new() { CustomMinimumSize = new Vector2(352, 90), Text = PieceName(id),
					Alignment = HorizontalAlignment.Left, ClipText = true, TooltipText = PieceName(id) };
				button.AddThemeConstantOverride("h_separation", 10);
				PatchworkPieceView thumbnail = new() { Position = new Vector2(10, 7), Size = new Vector2(86, 76), MouseFilter = MouseFilterEnum.Ignore };
				thumbnail.ShowPiece(id); button.AddChild(thumbnail);
				button.SetMeta("piece", id);
				button.Pressed += () => { if (_pending || _dragging) return; SelectInventory(pieceId); };
				button.GuiInput += input =>
				{
					if (input is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } || _pending) return;
					SelectInventory(pieceId);
					var first = PatchworkBoard.Cells(pieceId, 0, false)[0];
					BeginDrag(GetViewport().GetMousePosition(), new Vector2I(first.X, first.Y));
				};
				_pieces.AddChild(button);
			}
			if (state.AvailablePieces.Count == 0)
			{
				Label empty = new() { Text = Text("EMPTY"), AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(352, 80) };
				empty.AddThemeFontSizeOverride("font_size", 17); _pieces.AddChild(empty);
			}
		}
		foreach (Node child in _pieces.GetChildren())
			if (child is Button button)
			{
				PatchworkVisuals.StyleButton(button, _movingIndex < 0 && button.GetMeta("piece").AsInt32() == _selectedPiece);
				button.AddThemeFontSizeOverride("font_size", 17);
				foreach (string style in new[] { "normal", "hover", "pressed", "disabled" })
				{
					StyleBoxFlat box = (StyleBoxFlat)button.GetThemeStylebox(style);
					box.ContentMarginLeft = 106;
				}
				button.Disabled = _pending;
			}
		List<int> squares = valid ? PatchworkBoard.NewSquares(state, placement, _movingIndex) : [];
		_wiring.Present(state.ClaimedSquares, squares);
		_ancientChoice.Visible = squares.Contains(8) && !PreviewMode && _player.GetRelic<ArchaicTooth>() == null && _player.GetRelic<TouchOfOrobas>() == null;
		_ancientChoice.Disabled = _pending;
		_confirm.Disabled = !valid || _pending || _dragging ||
			(_movingIndex >= 0 && PatchworkBoard.Matches(placement, _originalPlacement!));
		_confirm.Text = Text(_movingIndex >= 0 ? "CONFIRM_MOVE" : "CONFIRM");
		_inventory.Text = Text("INVENTORY", ("Count", state.AvailablePieces.Count));
		_selected.Text = _selectedPiece < 0 ? Text("SELECT") : PieceName(_selectedPiece);
		_status.Text = _pending ? Text("SUBMITTED") : _selectedPiece < 0 ? Text("HELP") :
			valid ? Text("VALID", ("X", _x + 1), ("Y", _y + 1)) : Text("INVALID");
		_status.Modulate = _selectedPiece >= 0 && !valid ? PatchworkVisuals.Red : PatchworkVisuals.Cyan;
		PatchworkSquare? largest = PatchworkBoard.LargestCompletedSquare(state);
		_summary.Text = largest is { } square ? Text("LARGEST", ("Size", square.Size)) : Text("NO_SQUARE");
		foreach (var (size, row) in _rewardRows)
		{
			bool claimed = state.ClaimedSquares.Contains(size), preview = squares.Contains(size);
			Color color = claimed ? PatchworkVisuals.Cyan : preview ? PatchworkVisuals.Amber : new Color("486177");
			StyleBoxFlat box = PatchworkVisuals.Panel(new Color(claimed ? "102c38" : "10212b"), color, 4);
			box.ContentMarginTop = 4; box.ContentMarginBottom = 4;
			row.Row.AddThemeStyleboxOverride("panel", box);
			row.Led.AddThemeStyleboxOverride("panel", PatchworkVisuals.Panel(color, color, 5, claimed ? 8 : preview ? 5 : 0));
			row.State.Text = Text(claimed ? "UNLOCKED" : preview ? "PREVIEW_REWARD" : "LOCKED");
			row.State.Modulate = color;
		}
	}
	private PatchworkPlacement CurrentPlacement() => new() { PieceId = _selectedPiece, X = _x, Y = _y, Rotation = _rotation, Flipped = _flipped };
	private void Confirm()
	{
		if (_pending) return;
		PatchworkPlacement placement = CurrentPlacement();
		PatchworkSaveData state = BoardState;
		if (_dragging || !PatchworkBoard.CanPlace(state, placement, _movingIndex)) return;
		if (PreviewMode)
		{
			if (!PatchworkBoard.TryApply(state, placement, out _, _movingIndex, _originalPlacement)) return;
			_selectedPiece = -1; _movingIndex = -1; _originalPlacement = null;
			Refresh();
			return;
		}
		int choice = 0;
		if (PatchworkBoard.NewSquares(state, placement, _movingIndex).Contains(8))
		{
			bool tooth = _player.GetRelic<ArchaicTooth>() != null, touch = _player.GetRelic<TouchOfOrobas>() != null;
			choice = tooth && touch ? 0 : tooth ? 2 : touch ? 1 : _ancientChoice.GetSelectedId();
		}
		string before = StateKey(state);
		if (!PatchworkAccess.CanUse) return;
		bool requested = _movingIndex >= 0
			? PatchworkActions.RequestMove(_player, placement, _movingIndex, _originalPlacement!, choice)
			: PatchworkActions.RequestPlacement(_player, placement, choice);
		if (requested)
		{
			_pending = StateKey(BoardState) == before;
			_pendingSeconds = 0;
			_selectedPiece = -1; _movingIndex = -1; _originalPlacement = null;
			Refresh();
		}
	}
	public void AfterCapstoneOpened() => Refresh();
	public void AfterCapstoneClosed() => QueueFree();
}
