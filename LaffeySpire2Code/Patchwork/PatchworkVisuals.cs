using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public static class PatchworkVisuals
{
	public const string AssetRoot = "res://LaffeySpire2/images/patchwork/";
	public static readonly Color Cyan = new("65e4ff");
	public static readonly Color Amber = new("ffd17a");
	public static readonly Color Red = new("ff667b");
	private static readonly Color[] ColorsByType = [Cyan, Amber, Red, new("73efb5"), new("c49aff"), new("398dd4")];
	private static readonly Texture2D?[] Chips = new Texture2D?[34];
	private static readonly Texture2D?[] Icons = new Texture2D?[34];
	private static readonly Rect2[] ArtworkBounds = new Rect2[34];
	public static int ComponentType(int pieceId) => Math.Abs(pieceId) % 6;
	public static Color PieceColor(int pieceId) => ColorsByType[ComponentType(pieceId)];
	public static string Text(string key, params (string Name, object Value)[] values)
	{
		LocString text = new("gameplay_ui", "LAFFEY_PATCHWORK_" + key);
		foreach (var value in values)
		{
			if (value.Value is string stringValue) text.Add(value.Name, stringValue);
			else text.Add(value.Name, Convert.ToDecimal(value.Value));
		}
		return text.GetFormattedText();
	}
	public static string PieceName(int id) => Text(id == 0 ? "SHOP_MODULE" : "MODULE",
		("Type", Text("CHIP_" + id.ToString("D2"))), ("Id", id), ("Cells", PatchworkBoard.Cells(id, 0, false).Count));
	private static Texture2D Chip(int id)
	{
		if (Chips[id] != null) return Chips[id]!;
		Texture2D texture = ResourceLoader.Load<Texture2D>(AssetRoot + $"chips/chip_{id:D2}.png");
		using Image pixels = texture.GetImage();
		// Ignore almost-transparent fringe pixels so the complete package fills its logical footprint.
		// Source PNGs and their alpha are preserved; only the sampling rectangle changes.
		pixels.Convert(Image.Format.Rgba8);
		byte[] rgba = pixels.GetData();
		int imageWidth = pixels.GetWidth(), imageHeight = pixels.GetHeight();
		int left = imageWidth, top = imageHeight, right = -1, bottom = -1;
		for (int y = 0; y < imageHeight; y++)
		for (int x = 0; x < imageWidth; x++)
		{
			if (rgba[(y * imageWidth + x) * 4 + 3] < 26) continue;
			left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y);
		}
		Rect2I used = right >= left ? new Rect2I(left, top, right - left + 1, bottom - top + 1) : pixels.GetUsedRect();
		ArtworkBounds[id] = new Rect2(used.Position, used.Size);
		return Chips[id] = texture;
	}
	public static Texture2D Component(int id)
	{
		if (Icons[id] != null) return Icons[id]!;
		return Icons[id] = new AtlasTexture { Atlas = Chip(id), Region = ArtworkBounds[id], FilterClip = true };
	}

	/// <summary>One image per polyomino. Each cell samples its own part of that image, never a repeated tile.</summary>
	public static void DrawChip(Control canvas, int id, int rotation, bool flipped, Vector2 origin,
		float step, Color modulation, Color? outline = null, Rect2? clip = null)
	{
		var sourceCells = PatchworkBoard.Cells(id, 0, false);
		var placedCells = PatchworkBoard.Cells(id, rotation, flipped);
		if (sourceCells.Count == 0) return;
		Texture2D texture = Chip(id);
		Rect2 bounds = ArtworkBounds[id];
		int width = sourceCells.Max(c => c.X) + 1, height = sourceCells.Max(c => c.Y) + 1;
		Vector2 sourceStep = bounds.Size / new Vector2(width, height);
		for (int index = 0; index < sourceCells.Count; index++)
		{
			var target = placedCells[index];
			Vector2 position = origin + new Vector2(target.X, target.Y) * step;
			if (clip is { } limit && !limit.HasPoint(position + Vector2.One * step / 2)) continue;
			// The solid substrate joins seamlessly across logical cells; only the outside has a rim.
			canvas.DrawRect(new Rect2(position, Vector2.One * step), new Color(PieceColor(id).Darkened(0.6f), modulation.A));
			var source = sourceCells[index];
			Rect2 sourceRect = new(bounds.Position + new Vector2(source.X, source.Y) * sourceStep, sourceStep);
			canvas.DrawSetTransform(position + Vector2.One * step / 2, rotation * MathF.PI / 2, new Vector2(flipped ? -1 : 1, 1));
			canvas.DrawTextureRectRegion(texture, new Rect2(-Vector2.One * step / 2, Vector2.One * step), sourceRect, modulation);
			canvas.DrawSetTransform(Vector2.Zero);
		}
		var filled = placedCells.ToHashSet();
		Color rim = outline ?? new Color("d8e6ed");
		foreach (var cell in placedCells)
		{
			Vector2 pos = origin + new Vector2(cell.X, cell.Y) * step;
			if (clip is { } limit && !limit.HasPoint(pos + Vector2.One * step / 2)) continue;
			if (!filled.Contains((cell.X - 1, cell.Y))) canvas.DrawLine(pos, pos + new Vector2(0, step), rim, 1.2f);
			if (!filled.Contains((cell.X + 1, cell.Y))) canvas.DrawLine(pos + new Vector2(step, 0), pos + Vector2.One * step, rim, 1.2f);
			if (!filled.Contains((cell.X, cell.Y - 1))) canvas.DrawLine(pos, pos + new Vector2(step, 0), rim, 1.2f);
			if (!filled.Contains((cell.X, cell.Y + 1))) canvas.DrawLine(pos + new Vector2(0, step), pos + Vector2.One * step, rim, 1.2f);
		}
	}
	public static StyleBoxFlat Panel(Color background, Color border, int radius = 8, int glow = 0)
	{
		return new StyleBoxFlat
		{
			BgColor = background, BorderColor = border,
			BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
			CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius,
			CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
			ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8,
			ShadowColor = new Color(border, 0.35f), ShadowSize = glow
		};
	}
	public static void StyleButton(Button button, bool selected = false)
	{
		button.AddThemeStyleboxOverride("normal", Panel(new Color("10273d"), selected ? Cyan : new Color("31506a")));
		button.AddThemeStyleboxOverride("hover", Panel(new Color("163850"), Cyan, glow: 4));
		button.AddThemeStyleboxOverride("pressed", Panel(new Color("20506a"), Cyan));
		button.AddThemeStyleboxOverride("focus", Panel(Colors.Transparent, Amber));
		button.AddThemeStyleboxOverride("disabled", Panel(new Color("14202d"), new Color("293847")));
		button.AddThemeColorOverride("font_color", new Color("edf7ff"));
		button.AddThemeColorOverride("font_disabled_color", new Color("627588"));
		button.AddThemeFontSizeOverride("font_size", 18);
	}
}
