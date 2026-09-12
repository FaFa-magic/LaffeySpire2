using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Random;

namespace LaffeySpire2.LaffeySpire2Code.Nodes;

[GlobalClass]
public partial class LaffeyRestSiteCharacter : NRestSiteCharacter
{
	private static readonly System.Reflection.FieldInfo HitboxField =
		AccessTools.Field(typeof(NRestSiteCharacter), "<Hitbox>k__BackingField")
		?? throw new MissingFieldException(typeof(NRestSiteCharacter).FullName, "<Hitbox>k__BackingField");

	public override void _Ready()
	{
		// These skeletons use sleep instead of the vanilla rest-site animation names.
		var instance = Traverse.Create(this);
		instance.Field("_controlRoot").SetValue(GetNode<Control>("ControlRoot"));

		var hitbox = GetNode<Control>("%Hitbox");
		HitboxField.SetValue(this, hitbox);
		instance.Field("_selectionReticle").SetValue(GetNode<NSelectionReticle>("%SelectionReticle"));
		instance.Field("_leftThoughtAnchor").SetValue(GetNode<Control>("%ThoughtBubbleLeft"));
		instance.Field("_rightThoughtAnchor").SetValue(GetNode<Control>("%ThoughtBubbleRight"));

		var spineNode = GetNodeOrNull<Node2D>("SpineSprite");
		if (GodotObject.IsInstanceValid(spineNode) && spineNode.GetClass() == MegaSprite.spineClassName)
		{
			var sprite = new MegaSprite((Variant)(GodotObject)spineNode);
			this.RunWhenSpineReady(sprite, animationState =>
			{
				animationState.SetAnimation("sleep");
				using var track = animationState.GetCurrent(0);
				track?.SetTrackTime(track.GetAnimationEnd() * Rng.Chaotic.NextFloat());
			});
		}

		hitbox.Connect(Control.SignalName.FocusEntered, new Callable(this, "OnFocus"));
		hitbox.Connect(Control.SignalName.FocusExited, new Callable(this, "OnUnfocus"));
		hitbox.Connect(Control.SignalName.MouseEntered, new Callable(this, "OnFocus"));
		hitbox.Connect(Control.SignalName.MouseExited, new Callable(this, "OnUnfocus"));
	}
}
