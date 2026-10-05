using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.TopBar;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public sealed partial class PatchworkCatalogButton : NTopBarButton
{
	public bool PreviewMode { get; set; } = true;
	public Func<bool>? GalleryIsOpen { get; set; }
	public Func<HoverTip>? HoverTipProvider { get; set; }
	protected override string? ClickedSfx => PreviewMode ? null : base.ClickedSfx;
	protected override string? HoveredSfx => PreviewMode ? null : base.HoveredSfx;
	protected override bool IsOpen() => GalleryIsOpen?.Invoke() == true;

	public override void _Ready()
	{
		Control icon = GetNode<Control>("Control/Icon");
		icon.Material = new ShaderMaterial
		{
			Shader = new Shader { Code = """
				shader_type canvas_item;
				uniform float v = 1.0;
				void fragment() {
					COLOR.rgb *= v;
				}
				""" }
		};
		icon.Resized += () => icon.PivotOffset = icon.Size / 2;
		icon.PivotOffset = icon.Size / 2;
		InitTopBarButton();
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		if (PreviewMode || IsOpen() || HoverTipProvider == null || NGame.Instance?.HoverTipsContainer == null) return;
		NHoverTipSet.Remove(this);
		NHoverTipSet? tips = NHoverTipSet.CreateAndShow(this, HoverTipProvider());
		if (tips != null)
		{
			Rect2 bounds = GetGlobalRect();
			tips.SetGlobalPosition(bounds.Position + new Vector2(bounds.Size.X - tips.Size.X, bounds.Size.Y + 20));
		}
	}

	protected override void OnUnfocus()
	{
		base.OnUnfocus();
		NHoverTipSet.Remove(this);
	}

	public void RefreshScreenState()
	{
		UpdateScreenOpen();
		if (IsOpen()) NHoverTipSet.Remove(this);
	}

	public override void _ExitTree()
	{
		NHoverTipSet.Remove(this);
		base._ExitTree();
	}
}
