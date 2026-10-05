using Godot;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public partial class PatchworkChipGallery : Control
{
	[Export] public bool PreviewMode { get; set; } = true;
	[Export] public string PreviewLanguage { get; set; } = "zhs";
	public event Action? CloseRequested;
	public Func<PatchworkSaveData>? StateProvider { get; set; }
	public Button CloseButton { get; private set; } = null!;
	private Control _workspace = null!;
	private readonly Dictionary<int, (PatchworkPieceView Chip, Label Label)> _chips = [];
	private HashSet<int>? _obtainedPieces;
	private double _pollSeconds;
	private readonly ShaderMaterial _unobtainedMaterial = new()
	{
		Shader = new Shader { Code = """
			shader_type canvas_item;
			void fragment() {
				vec4 source = COLOR;
				float gray = dot(source.rgb, vec3(0.299, 0.587, 0.114));
				COLOR = vec4(vec3(gray * 0.82), source.a);
			}
			""" }
	};
	private Dictionary<string, string> _previewText = [];
	private string Text(string key) => PreviewMode
		? _previewText.GetValueOrDefault("LAFFEY_PATCHWORK_" + key, key)
		: PatchworkVisuals.Text(key);
	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Stop;
		if (PreviewMode)
		{
			var language = PreviewLanguage == "eng" ? "eng" : "zhs";
			_previewText = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
				Godot.FileAccess.GetFileAsString($"res://LaffeySpire2/localization/{language}/gameplay_ui.json"))!;
		}
		TextureRect background = new() { Texture = ResourceLoader.Load<Texture2D>(PatchworkVisuals.AssetRoot + "workbench-v2.png"),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, MouseFilter = MouseFilterEnum.Ignore };
		AddChild(background); background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		ColorRect dim = new() { Color = new Color(0.015f, 0.03f, 0.045f, 0.65f), MouseFilter = MouseFilterEnum.Ignore };
		AddChild(dim); dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_workspace = new Control { Name = "Workspace", Size = new Vector2(1680, 1000), MouseFilter = MouseFilterEnum.Ignore }; AddChild(_workspace);
		Label title = new() { Text = Text("CHIP_GALLERY"), Position = new Vector2(100, 140), Size = new Vector2(1480, 44), HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
		title.AddThemeFontSizeOverride("font_size", 30); _workspace.AddChild(title);
		CloseButton = new Button { Name = "Close", Text = "×", TooltipText = Text("GALLERY_CLOSE"),
			Position = new Vector2(1580, 134), Size = new Vector2(50, 50) };
		PatchworkVisuals.StyleButton(CloseButton, false);
		CloseButton.AddThemeFontSizeOverride("font_size", 30);
		_workspace.AddChild(CloseButton);
		CloseButton.FocusNeighborLeft = CloseButton.FocusNeighborRight = CloseButton.FocusNeighborTop =
			CloseButton.FocusNeighborBottom = CloseButton.FocusNext = CloseButton.FocusPrevious = CloseButton.GetPath();
		CloseButton.Pressed += RequestClose;
		for (int id = 0; id < 34; id++)
		{
			Panel panel = new() { Name = "Chip" + id.ToString("D2"), Position = new Vector2(50 + id % 8 * 198, 204 + id / 8 * 150), Size = new Vector2(190, 142), MouseFilter = MouseFilterEnum.Ignore };
			panel.AddThemeStyleboxOverride("panel", PatchworkVisuals.Panel(new Color("0b1a28"), new Color("536777"), 5)); _workspace.AddChild(panel);
			PatchworkPieceView chip = new() { Name = "Artwork", Position = new Vector2(8, 7), Size = new Vector2(174, 106), MouseFilter = MouseFilterEnum.Ignore };
			panel.AddChild(chip); chip.ShowPiece(id);
			Label label = new() { Text = $"{id:D2} · " + Text("CHIP_" + id.ToString("D2")), Position = new Vector2(5, 115), Size = new Vector2(180, 22), HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
			label.AddThemeFontSizeOverride("font_size", 14); panel.AddChild(label);
			_chips[id] = (chip, label);
		}
		RefreshObtainedPieces();
		Resized += Fit; Fit();
		CloseButton.GrabFocus();
	}
	public override void _Process(double delta)
	{
		_pollSeconds += delta;
		if (_pollSeconds < 0.15) return;
		_pollSeconds = 0;
		RefreshObtainedPieces();
	}
	private void RefreshObtainedPieces()
	{
		HashSet<int> obtained = StateProvider != null ? PatchworkBoard.ObtainedPieces(StateProvider()) : _chips.Keys.ToHashSet();
		if (_obtainedPieces != null && _obtainedPieces.SetEquals(obtained)) return;
		_obtainedPieces = obtained;
		foreach (var (id, view) in _chips)
		{
			bool known = obtained.Contains(id);
			view.Chip.Material = known ? null : _unobtainedMaterial;
			view.Chip.QueueRedraw();
			view.Label.AddThemeColorOverride("font_color", known ? Colors.White : new Color("9ca9b3"));
		}
	}
	public override void _Input(InputEvent input)
	{
		if (input is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape }) return;
		GetViewport().SetInputAsHandled();
		RequestClose();
	}
	private void RequestClose()
	{
		if (CloseRequested != null) CloseRequested.Invoke();
		else QueueFree();
	}
	private void Fit()
	{
		float scale = Math.Max(0.1f, Math.Min(1.18f, Math.Min((Size.X - 24) / 1680f, (Size.Y - 24) / 1000f)));
		_workspace.Scale = Vector2.One * scale;
		_workspace.Position = (Size - new Vector2(1680, 1000) * scale) / 2;
	}
}
