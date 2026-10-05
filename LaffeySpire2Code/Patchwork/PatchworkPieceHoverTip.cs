using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public partial class PatchworkPieceHoverTip : Node
{
	public bool PreviewMode { get; set; }
	public Func<int> PieceIdProvider { get; set; } = null!;
	public Func<bool> CanShow { get; set; } = () => true;
	private Control _owner = null!;
	private int _shownPiece = -1;

	public override void _Ready() => _owner = GetParent<Control>();

	public override void _Process(double delta)
	{
		bool focused = _owner.HasFocus() && NControllerManager.Instance?.IsUsingDirectionalNavigation == true;
		bool hovered = GetViewport().GuiGetHoveredControl() == _owner;
		int piece = !PreviewMode && _owner.IsVisibleInTree() && CanShow() && (hovered || focused) &&
			!NHoverTipSet.shouldBlockHoverTips && NGame.Instance?.HoverTipsContainer != null &&
			(_owner is not BaseButton button || !button.Disabled && !button.IsPressed())
			? PieceIdProvider() : -1;
		if (piece is < 0 or > 33) piece = -1;
		if (piece == _shownPiece) return;
		NHoverTipSet.Remove(_owner);
		_shownPiece = -1;
		if (piece >= 0 && NHoverTipSet.CreateAndShow(_owner, PatchworkVisuals.PieceHoverTip(piece),
			HoverTipAlignment.Left) != null)
			_shownPiece = piece;
	}

	public override void _ExitTree()
	{
		if (_owner != null) NHoverTipSet.Remove(_owner);
	}
}
