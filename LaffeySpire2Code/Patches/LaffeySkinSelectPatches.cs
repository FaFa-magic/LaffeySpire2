using Godot;
using HarmonyLib;
using LaffeySpire2.LaffeySpire2Code.Characters;
using LaffeySpire2.LaffeySpire2Code.Nodes;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using STS2RitsuLib.Patching.Models;

namespace LaffeySpire2.LaffeySpire2Code.Patches;

internal static class LaffeySkinSelectPanelController
{
	private const string ScenePath = "res://LaffeySpire2/scenes/ui/laffey_skin_select_panel.tscn";
	private static LaffeySkinSelectPanel? _panelInstance;

	public static void OnCharacterSelected(NCharacterSelectScreen screen, CharacterModel character)
	{
		if (character is not LaffeyCharacter laffeySkin)
		{
			if (GodotObject.IsInstanceValid(_panelInstance))
				_panelInstance.Visible = false;
			return;
		}

		var infoPanel = screen.GetNodeOrNull<Control>("%InfoPanel");
		if (infoPanel is null)
			return;

		if (!GodotObject.IsInstanceValid(_panelInstance) || _panelInstance.GetParent() != infoPanel)
		{
			if (GodotObject.IsInstanceValid(_panelInstance))
				_panelInstance.QueueFreeSafely();

			var scene = ResourceLoader.Load<PackedScene>(ScenePath);
			if (scene is null)
				return;

			_panelInstance = scene.Instantiate<LaffeySkinSelectPanel>(PackedScene.GenEditState.Disabled);
			infoPanel.AddChildSafely(_panelInstance);
			_panelInstance.Position = new Vector2(400f, 0f);
		}

		_panelInstance.SetInteractable(true);
		_panelInstance.ShowAndSync(screen, laffeySkin);
	}

	public static void SetInteractable(bool interactable)
	{
		if (GodotObject.IsInstanceValid(_panelInstance))
			_panelInstance.SetInteractable(interactable);
	}
}

public sealed class LaffeySkinSelectPatch : IPatchMethod
{
	public static string PatchId => "laffey_skin_select_panel";
	public static string Description => "Show the Laffey Spine skin selector";
	public static bool IsCritical => false;

	public static ModPatchTarget[] GetTargets() =>
	[
		new(
			typeof(NCharacterSelectScreen),
			nameof(NCharacterSelectScreen.SelectCharacter),
			[typeof(NCharacterSelectButton), typeof(CharacterModel)])
	];

	[HarmonyPostfix]
	public static void Postfix(NCharacterSelectScreen __instance, CharacterModel characterModel)
	{
		LaffeySkinSelectPanelController.OnCharacterSelected(__instance, characterModel);
	}
}

public sealed class LaffeySkinSelectEmbarkPatch : IPatchMethod
{
	public static string PatchId => "laffey_skin_select_panel_embark";
	public static string Description => "Lock the Laffey skin selector while embarking";
	public static bool IsCritical => false;
	public static ModPatchTarget[] GetTargets() => [new(typeof(NCharacterSelectScreen), "OnEmbarkPressed", null)];

	[HarmonyPostfix]
	public static void Postfix() => LaffeySkinSelectPanelController.SetInteractable(false);
}

public sealed class LaffeySkinSelectUnreadyPatch : IPatchMethod
{
	public static string PatchId => "laffey_skin_select_panel_unready";
	public static string Description => "Unlock the Laffey skin selector after unreadying";
	public static bool IsCritical => false;
	public static ModPatchTarget[] GetTargets() => [new(typeof(NCharacterSelectScreen), "OnUnreadyPressed", null)];

	[HarmonyPostfix]
	public static void Postfix() => LaffeySkinSelectPanelController.SetInteractable(true);
}
