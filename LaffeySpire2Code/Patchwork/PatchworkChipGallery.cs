using Godot;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

/// <summary>Standalone art contact sheet using the same whole-chip renderer as gameplay.</summary>
public partial class PatchworkChipGallery : Control
{
	[Export] public string PreviewLanguage { get; set; } = "zhs";
	private Control _workspace = null!;
	public override void _Ready()
	{
		var language = PreviewLanguage == "eng" ? "eng" : "zhs";
		var text = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
			Godot.FileAccess.GetFileAsString($"res://LaffeySpire2/localization/{language}/gameplay_ui.json"))!;
		TextureRect background = new() { Texture = ResourceLoader.Load<Texture2D>(PatchworkVisuals.AssetRoot + "workbench-v2.png"),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, MouseFilter = MouseFilterEnum.Ignore };
		AddChild(background); background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		ColorRect dim = new() { Color = new Color(0.015f, 0.03f, 0.045f, 0.65f), MouseFilter = MouseFilterEnum.Ignore };
		AddChild(dim); dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_workspace = new Control { Size = new Vector2(1680, 1000) }; AddChild(_workspace);
		Label title = new() { Text = text["LAFFEY_PATCHWORK_CHIP_GALLERY"], Position = new Vector2(50, 36), Size = new Vector2(1580, 44), HorizontalAlignment = HorizontalAlignment.Center };
		title.AddThemeFontSizeOverride("font_size", 30); _workspace.AddChild(title);
		for (int id = 0; id < 34; id++)
		{
			Panel panel = new() { Position = new Vector2(50 + id % 8 * 198, 110 + id / 8 * 174), Size = new Vector2(190, 166), MouseFilter = MouseFilterEnum.Ignore };
			panel.AddThemeStyleboxOverride("panel", PatchworkVisuals.Panel(new Color("0b1a28"), new Color("536777"), 5)); _workspace.AddChild(panel);
			PatchworkPieceView chip = new() { Position = new Vector2(8, 7), Size = new Vector2(174, 130), MouseFilter = MouseFilterEnum.Ignore };
			panel.AddChild(chip); chip.ShowPiece(id);
			Label label = new() { Text = $"{id:D2} · " + text["LAFFEY_PATCHWORK_CHIP_" + id.ToString("D2")], Position = new Vector2(5, 139), Size = new Vector2(180, 22), HorizontalAlignment = HorizontalAlignment.Center };
			label.AddThemeFontSizeOverride("font_size", 14); panel.AddChild(label);
		}
		Resized += Fit; Fit();
	}
	private void Fit()
	{
		float scale = Math.Min((Size.X - 24) / 1680f, (Size.Y - 24) / 1000f);
		_workspace.Scale = Vector2.One * scale;
		_workspace.Position = (Size - new Vector2(1680, 1000) * scale) / 2;
	}
}
