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

[RegisterOwnedTopBarButton("patchwork", ButtonOrder = 0)]
public sealed class PatchworkTopBarButton : IModTopBarButtonHandler
{
	public void OnClick(ModTopBarButtonContext context)
	{
		if (context.Player is not { Character: LaffeyCharacter } player)
			return;
		if (ModScreenService.CurrentCapstoneScreen is PatchworkScreen)
			context.CloseCapstoneScreen();
		else
			context.OpenCapstoneScreen(PatchworkScreen.Create(player));
	}

	public bool IsVisible(ModTopBarButtonContext context) =>
		context.Player is { Character: LaffeyCharacter } player &&
		player.RunState.CurrentRoom is not MegaCrit.Sts2.Core.Rooms.CombatRoom;

	public bool IsOpen(ModTopBarButtonContext context) =>
		ModScreenService.CurrentCapstoneScreen is PatchworkScreen;

	public int GetCount(ModTopBarButtonContext context) => context.Player is { Character: LaffeyCharacter } player
		? PatchworkBoard.Get(player).AvailablePieces.Count
		: -1;
}

public sealed partial class PatchworkScreen : Control, ICapstoneScreen
{
	private Player _player = null!;
	private GridContainer _grid = null!;
	private VBoxContainer _pieces = null!;
	private Label _status = null!;
	private Label _shapePreview = null!;
	private Label _milestones = null!;
	private Button _confirm = null!;
	private Button _close = null!;
	private OptionButton _ancientChoice = null!;
	private int _selectedPiece = -1;
	private int _rotation;
	private bool _flipped;
	private int _x;
	private int _y;
	private int _lastStateHash;

	public static PatchworkScreen Create(Player player)
	{
		return new PatchworkScreen { _player = player };
	}

	public NetScreenType ScreenType => NetScreenType.CardPile;
	public bool UseSharedBackstop => true;
	public Control? DefaultFocusedControl => _close;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		CenterContainer center = new();
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(center);
		PanelContainer panel = new()
		{
			CustomMinimumSize = new Vector2(850, 650),
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
			SizeFlagsVertical = SizeFlags.ShrinkCenter
		};
		center.AddChild(panel);
		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", 24);
		margin.AddThemeConstantOverride("margin_right", 24);
		margin.AddThemeConstantOverride("margin_top", 20);
		margin.AddThemeConstantOverride("margin_bottom", 20);
		panel.AddChild(margin);
		VBoxContainer root = new();
		root.AddThemeConstantOverride("separation", 12);
		margin.AddChild(root);
		Label title = new() { Text = "拉菲的拼布板", HorizontalAlignment = HorizontalAlignment.Center };
		title.AddThemeFontSizeOverride("font_size", 28);
		root.AddChild(title);
		HBoxContainer body = new();
		body.AddThemeConstantOverride("separation", 24);
		root.AddChild(body);
		_grid = new GridContainer { Columns = 10 };
		_grid.AddThemeConstantOverride("h_separation", 2);
		_grid.AddThemeConstantOverride("v_separation", 2);
		body.AddChild(_grid);
		VBoxContainer sidebar = new() { CustomMinimumSize = new Vector2(290, 440) };
		body.AddChild(sidebar);
		sidebar.AddChild(new Label { Text = "待放置拼图" });
		ScrollContainer scroll = new() { CustomMinimumSize = new Vector2(280, 265), SizeFlagsVertical = SizeFlags.ExpandFill };
		sidebar.AddChild(scroll);
		_pieces = new VBoxContainer();
		scroll.AddChild(_pieces);
		_shapePreview = new Label { HorizontalAlignment = HorizontalAlignment.Center, CustomMinimumSize = new Vector2(0, 82) };
		_shapePreview.AddThemeFontSizeOverride("font_size", 20);
		sidebar.AddChild(_shapePreview);
		HBoxContainer tools = new();
		sidebar.AddChild(tools);
		Button rotate = new() { Text = "旋转 90°" };
		rotate.Pressed += () => { _rotation = (_rotation + 1) % 4; Refresh(); };
		tools.AddChild(rotate);
		Button flip = new() { Text = "翻转" };
		flip.Pressed += () => { _flipped = !_flipped; Refresh(); };
		tools.AddChild(flip);
		_ancientChoice = new OptionButton();
		_ancientChoice.AddItem("古老牙齿", 1);
		_ancientChoice.AddItem("欧罗巴斯之触", 2);
		_ancientChoice.Select(0);
		sidebar.AddChild(_ancientChoice);
		_status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		sidebar.AddChild(_status);
		_milestones = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		sidebar.AddChild(_milestones);
		HBoxContainer bottom = new();
		root.AddChild(bottom);
		_confirm = new Button { Text = "确认放置（不可撤销）", SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_confirm.Pressed += Confirm;
		bottom.AddChild(_confirm);
		_close = new Button { Text = "关闭" };
		_close.Pressed += () => ModScreenService.Close();
		bottom.AddChild(_close);
		Refresh();
	}

	public override void _Process(double delta)
	{
		PatchworkSaveData state = PatchworkBoard.Get(_player);
		int hash = HashCode.Combine(state.AvailablePieces.Count, state.Placements.Count, state.ClaimedSquares.Count);
		if (hash == _lastStateHash)
			return;
		_lastStateHash = hash;
		Refresh();
	}

	private void Refresh()
	{
		if (_grid == null)
			return;
		PatchworkSaveData state = PatchworkBoard.Get(_player);
		foreach (Node child in _grid.GetChildren())
		{
			_grid.RemoveChild(child);
			child.QueueFree();
		}
		foreach (Node child in _pieces.GetChildren())
		{
			_pieces.RemoveChild(child);
			child.QueueFree();
		}
		bool[,] occupied = PatchworkBoard.Occupied(state);
		PatchworkPlacement placement = CurrentPlacement();
		bool valid = _selectedPiece >= 0 && PatchworkBoard.CanPlace(state, placement);
		HashSet<(int, int)> preview = valid
			? PatchworkBoard.Cells(_selectedPiece, _rotation, _flipped)
				.Select(cell => (_x + cell.X, _y + cell.Y)).ToHashSet()
			: [];
		for (int y = 0; y < 10; y++)
		for (int x = 0; x < 10; x++)
		{
			int bx = x;
			int by = y;
			Button cell = new()
			{
				CustomMinimumSize = new Vector2(39, 39),
				Text = occupied[x, y] ? "■" : preview.Contains((x, y)) ? "◆" : "·",
				Modulate = occupied[x, y] ? new Color("#78b9db") : preview.Contains((x, y)) ? new Color("#a8e5a1") : Colors.White
			};
			cell.Pressed += () => { _x = bx; _y = by; Refresh(); };
			_grid.AddChild(cell);
		}
		foreach (int id in state.AvailablePieces)
		{
			int pieceId = id;
			Button button = new() { Text = $"{(id == 0 ? "商店补丁" : $"拼图 {id}")}  ({PatchworkBoard.Cells(id, 0, false).Count} 格)" };
			button.Modulate = id == _selectedPiece ? new Color("#a8e5a1") : Colors.White;
			button.Pressed += () => { _selectedPiece = pieceId; _rotation = 0; _flipped = false; Refresh(); };
			_pieces.AddChild(button);
		}
		List<int> squares = valid ? PatchworkBoard.NewSquares(state, placement) : [];
		bool needsChoice = squares.Contains(8) &&
			_player.GetRelic<ArchaicTooth>() == null && _player.GetRelic<TouchOfOrobas>() == null;
		_ancientChoice.Visible = needsChoice;
		_confirm.Disabled = !valid;
		_shapePreview.Text = _selectedPiece < 0 ? "" : ShapeText(_selectedPiece);
		_status.Text = _selectedPiece < 0 ? "选择拼图，再点击底板确定左上角。" :
			valid ? $"预览：拼图 {_selectedPiece}，坐标 {_x + 1},{_y + 1}。" : "该位置无法放置。";
		_milestones.Text = "已领取奖励：" + (state.ClaimedSquares.Count == 0
			? "无" : string.Join("、", state.ClaimedSquares.OrderBy(size => size).Select(size => $"{size}×{size}")));
	}

	private string ShapeText(int pieceId)
	{
		IReadOnlyList<(int X, int Y)> cells = PatchworkBoard.Cells(pieceId, _rotation, _flipped);
		int width = cells.Max(cell => cell.X) + 1;
		int height = cells.Max(cell => cell.Y) + 1;
		HashSet<(int, int)> filled = cells.ToHashSet();
		return string.Join("\n", Enumerable.Range(0, height).Select(y =>
			string.Join("", Enumerable.Range(0, width).Select(x => filled.Contains((x, y)) ? "■ " : "· "))));
	}

	private PatchworkPlacement CurrentPlacement() => new()
	{
		PieceId = _selectedPiece, X = _x, Y = _y, Rotation = _rotation, Flipped = _flipped
	};

	private void Confirm()
	{
		PatchworkPlacement placement = CurrentPlacement();
		PatchworkSaveData state = PatchworkBoard.Get(_player);
		if (!PatchworkBoard.CanPlace(state, placement))
			return;
		int choice = 0;
		if (PatchworkBoard.NewSquares(state, placement).Contains(8))
		{
			bool tooth = _player.GetRelic<ArchaicTooth>() != null;
			bool touch = _player.GetRelic<TouchOfOrobas>() != null;
			choice = tooth && touch ? 0 : tooth ? 2 : touch ? 1 : _ancientChoice.GetSelectedId();
		}
		if (PatchworkActions.RequestPlacement(_player, placement, choice))
		{
			_selectedPiece = -1;
			_confirm.Disabled = true;
			_status.Text = "放置请求已提交。";
		}
	}

	public void AfterCapstoneOpened() => Refresh();

	public void AfterCapstoneClosed() => QueueFree();
}
