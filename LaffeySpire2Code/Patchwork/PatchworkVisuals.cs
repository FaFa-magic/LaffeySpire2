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
	public static int ComponentType(int pieceId) => Math.Abs(pieceId) % 6;
	public static Color PieceColor(int pieceId) => ColorsByType[ComponentType(pieceId)];
	public static string Text(string key, params (string Name, object Value)[] values)
		=> LocalizedText(key, values).GetFormattedText();
	public static LocString LocalizedText(string key, params (string Name, object Value)[] values)
	{
		LocString text = new("gameplay_ui", "LAFFEY_PATCHWORK_" + key);
		foreach (var value in PatchworkBalance.TextValues(key).Concat(values))
		{
			if (value.Value is string stringValue) text.Add(value.Name, stringValue);
			else text.Add(value.Name, Convert.ToDecimal(value.Value));
		}
		return text;
	}
	public static string PieceName(int id) => Text(id == 0 ? "SHOP_MODULE" : "MODULE",
		("Type", Text("CHIP_" + id.ToString("D2"))), ("Id", id), ("Cells", PatchworkBoard.Cells(id, 0, false).Count));
	private static Texture2D Chip(int id)
	{
		if (Chips[id] != null) return Chips[id]!;
		return Chips[id] = ResourceLoader.Load<Texture2D>(AssetRoot + $"chips/chip_{id:D2}.png");
	}
	public static Texture2D Component(int id) => Chip(id);

	/// <summary>One square source unit, even if a future asset has an incorrect aspect ratio.</summary>
	public static float SourceCellSize(Vector2 textureSize, int columns, int rows) =>
		Math.Max(textureSize.X / columns, textureSize.Y / rows);

	/// <summary>Draw the complete polyomino once so its metal rim and rounded cutouts remain intact.</summary>
	public static void DrawChip(Control canvas, int id, int rotation, bool flipped, Vector2 origin,
		float step, Color modulation, Color? outline = null, Rect2? clip = null)
	{
		var sourceCells = PatchworkBoard.Cells(id, 0, false);
		var placedCells = PatchworkBoard.Cells(id, rotation, flipped);
		if (sourceCells.Count == 0) return;
		Texture2D texture = Chip(id);
		int width = sourceCells.Max(c => c.X) + 1, height = sourceCells.Max(c => c.Y) + 1;
		// PNG canvas dimensions match the logical grid, including transparent missing cells.
		// Never crop alpha bounds and independently resize X/Y to compensate for malformed art.
		float sourceUnit = SourceCellSize(texture.GetSize(), width, height);
		Vector2 sourceOrigin = (texture.GetSize() - new Vector2(width, height) * sourceUnit) / 2;
		float scale = step / sourceUnit;
		// Explicit R * mirror basis matches Cells(): mirror first, then rotate.
		float angle = rotation * MathF.PI / 2;
		Vector2 basisX = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (flipped ? -scale : scale);
		Vector2 basisY = new Vector2(-MathF.Sin(angle), MathF.Cos(angle)) * scale;
		Vector2[] corners = [Vector2.Zero, basisX * width * sourceUnit, basisY * height * sourceUnit,
			basisX * width * sourceUnit + basisY * height * sourceUnit];
		Vector2 minimum = new(corners.Min(c => c.X), corners.Min(c => c.Y));
		Transform2D transform = new(basisX, basisY,
			origin - minimum - basisX * sourceOrigin.X - basisY * sourceOrigin.Y);
		Rect2 sourceRect = new(Vector2.Zero, texture.GetSize());
		if (clip is { } limit)
		{
			// Quarter turns keep the board clip axis aligned in source space.
			Transform2D inverse = transform.AffineInverse();
			Vector2[] clipCorners = [inverse * limit.Position, inverse * limit.End,
				inverse * (limit.Position + new Vector2(limit.Size.X, 0)),
				inverse * (limit.Position + new Vector2(0, limit.Size.Y))];
			Vector2 start = new(clipCorners.Min(c => c.X), clipCorners.Min(c => c.Y));
			Vector2 end = new(clipCorners.Max(c => c.X), clipCorners.Max(c => c.Y));
			sourceRect = sourceRect.Intersection(new Rect2(start, end - start));
		}
		if (sourceRect.Size.X > 0 && sourceRect.Size.Y > 0)
		{
			canvas.DrawSetTransformMatrix(transform);
			// Equal source/destination rectangles; the single scalar transform is the only scaling.
			canvas.DrawTextureRectRegion(texture, sourceRect, sourceRect, modulation);
			canvas.DrawSetTransform(Vector2.Zero);
		}
		// Ordinary chips use their painted metal perimeter. Only placement previews need an overlay.
		if (outline is not { } rim) return;
		var filled = placedCells.ToHashSet();
		float corner = step * 0.08f;
		foreach (var cell in placedCells)
		{
			Vector2 pos = origin + new Vector2(cell.X, cell.Y) * step;
			if (clip is { } outlineLimit && !outlineLimit.HasPoint(pos + Vector2.One * step / 2)) continue;
			bool left = !filled.Contains((cell.X - 1, cell.Y)), right = !filled.Contains((cell.X + 1, cell.Y));
			bool top = !filled.Contains((cell.X, cell.Y - 1)), bottom = !filled.Contains((cell.X, cell.Y + 1));
			if (left) canvas.DrawLine(pos + new Vector2(0, top ? corner : 0), pos + new Vector2(0, step - (bottom ? corner : 0)), rim, 1.2f, true);
			if (right) canvas.DrawLine(pos + new Vector2(step, top ? corner : 0), pos + new Vector2(step, step - (bottom ? corner : 0)), rim, 1.2f, true);
			if (top) canvas.DrawLine(pos + new Vector2(left ? corner : 0, 0), pos + new Vector2(step - (right ? corner : 0), 0), rim, 1.2f, true);
			if (bottom) canvas.DrawLine(pos + new Vector2(left ? corner : 0, step), pos + new Vector2(step - (right ? corner : 0), step), rim, 1.2f, true);
			if (left && top) canvas.DrawLine(pos + new Vector2(0, corner), pos + new Vector2(corner, 0), rim, 1.2f, true);
			if (right && top) canvas.DrawLine(pos + new Vector2(step - corner, 0), pos + new Vector2(step, corner), rim, 1.2f, true);
			if (left && bottom) canvas.DrawLine(pos + new Vector2(0, step - corner), pos + new Vector2(corner, step), rim, 1.2f, true);
			if (right && bottom) canvas.DrawLine(pos + new Vector2(step - corner, step), pos + new Vector2(step, step - corner), rim, 1.2f, true);
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
