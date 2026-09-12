using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace LaffeySpire2.LaffeySpire2Code.Nodes;

[GlobalClass]
public partial class LaffeyMerchantCharacter : NMerchantCharacter
{
	public override void _Ready()
	{
		var spineNode = GetNodeOrNull<Node2D>("SpineSprite");
		if (!GodotObject.IsInstanceValid(spineNode) || spineNode.GetClass() != MegaSprite.spineClassName)
			return;

		var sprite = new MegaSprite((Variant)(GodotObject)spineNode);
		this.RunWhenSpineReady(sprite, _ => PlayAnimation("normal", true));
	}
}
