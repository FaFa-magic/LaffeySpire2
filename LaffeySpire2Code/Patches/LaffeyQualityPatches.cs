using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using LaffeySpire2.LaffeySpire2Code.CardQuality;
using LaffeySpire2.LaffeySpire2Code.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using STS2RitsuLib.Patching.Models;

namespace LaffeySpire2.LaffeySpire2Code.Patches;

public sealed class LaffeyQualityUpgradePatch : IPatchMethod
{
	public static string PatchId => "laffey_quality_upgrade_configuration";
	public static string Description => "Keep quality configuration independent of ordinary upgrades";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(CardModel), nameof(CardModel.UpgradeInternal), []),
		new(typeof(CardModel), nameof(CardModel.DowngradeInternal), [])
	];

	[HarmonyPostfix]
	public static void Postfix(CardModel __instance, System.Reflection.MethodBase __originalMethod)
	{
		if (__instance is LaffeyCardModel card)
			card.RefreshQualityConfiguration(__originalMethod.Name == nameof(CardModel.DowngradeInternal));
	}
}

internal sealed class QualityInspectorState
{
	internal bool Enabled;
	internal CardModel? Source;
	internal int Rank;
	internal NGoldArrowButton? Left;
	internal NGoldArrowButton? Right;
	internal static ConditionalWeakTable<NInspectCardScreen, QualityInspectorState> States { get; } = new();
	internal static QualityInspectorState Get(NInspectCardScreen screen)
		=> States.GetValue(screen, static _ => new QualityInspectorState());

	internal static void Refresh(NInspectCardScreen screen)
		=> AccessTools.Method(typeof(NInspectCardScreen), "UpdateCardDisplay").Invoke(screen, null);

	internal void Hide()
	{
		Left?.Hide();
		Right?.Hide();
		Left?.Disable();
		Right?.Disable();
	}

	internal void CreateControls(NInspectCardScreen screen, NButton leftSource, NButton rightSource)
	{
		if (Left != null)
			return;
		Left = DuplicateArrow(leftSource, "LaffeyQualityLeft");
		Right = DuplicateArrow(rightSource, "LaffeyQualityRight");
		screen.AddChild(Left);
		screen.AddChild(Right);
		Left.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => ChangeRank(screen, -1)));
		Right.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => ChangeRank(screen, 1)));
	}

	private static NGoldArrowButton DuplicateArrow(NButton source, string name)
	{
		const Node.DuplicateFlags flags = Node.DuplicateFlags.Groups |
			Node.DuplicateFlags.Scripts | Node.DuplicateFlags.UseInstantiation;
		NGoldArrowButton arrow = (NGoldArrowButton)source.Duplicate((int)flags);
		arrow.Name = name;
		arrow.Scale = Vector2.One * 0.5f;
		arrow.Modulate = Colors.White;
		TextureRect icon = arrow.GetNode<TextureRect>("TextureRect");
		icon.Material = (Material)icon.Material.Duplicate();
		return arrow;
	}

	private void ChangeRank(NInspectCardScreen screen, int change)
	{
		if (!Enabled || !screen.Visible)
			return;
		Rank = Math.Clamp(Rank + change, 1, 3);
		Refresh(screen);
	}
}

public sealed class LaffeyQualityInspectorContextPatch : IPatchMethod
{
	public static string PatchId => "laffey_quality_inspector_context";
	public static string Description => "Reset quality browsing when opening or closing an inspector";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NInspectCardScreen), nameof(NInspectCardScreen.Open), [typeof(List<CardModel>), typeof(int), typeof(bool)]),
		new(typeof(NInspectCardScreen), nameof(NInspectCardScreen.Close), [])
	];

	[HarmonyPrefix]
	public static void Prefix(NInspectCardScreen __instance)
	{
		QualityInspectorState state = QualityInspectorState.Get(__instance);
		state.Enabled = false;
		state.Source = null;
		state.Hide();
	}
}

public sealed class LaffeyQualityLibraryPatch : IPatchMethod
{
	public static string PatchId => "laffey_quality_card_library";
	public static string Description => "Enable quality browsing only from the card library";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NCardLibrary), "ShowCardDetail", [typeof(MegaCrit.Sts2.Core.Nodes.Cards.Holders.NCardHolder)])
	];

	[HarmonyPostfix]
	public static void Postfix()
	{
		NInspectCardScreen? screen = NGame.Instance?.GetInspectCardScreen();
		if (screen == null || !screen.Visible)
			return;
		QualityInspectorState.Get(screen).Enabled = true;
		QualityInspectorState.Refresh(screen);
	}
}

public sealed class LaffeyQualityInspectorPatch : IPatchMethod
{
	public static string PatchId => "laffey_quality_inspector_controls";
	public static string Description => "Browse Common, Uncommon and Rare versions below the card navigation";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() => [new(typeof(NInspectCardScreen), "UpdateCardDisplay", [])];

	[HarmonyPostfix]
	public static void Postfix(NInspectCardScreen __instance, List<CardModel> ____cards, int ____index,
		NCard ____card, NButton ____leftButton, NButton ____rightButton, Control ____hoverTipRect,
		NTickbox ____upgradeTickbox)
	{
		QualityInspectorState state = QualityInspectorState.Get(__instance);
		if (!state.Enabled || ____cards[____index] is not LaffeyCardModel { HasQualityVersions: true } source
			|| ____card.Model is not LaffeyCardModel preview)
		{
			state.Hide();
			state.Source = null;
			return;
		}
		if (!ReferenceEquals(state.Source, source))
		{
			state.Source = source;
			state.Rank = LaffeyQuality.RankForRarity(source.NativeRarity);
		}
		preview.QualityRank = state.Rank;
		____card.Model = null;
		____card.Model = preview;
		if (____upgradeTickbox.IsTicked)
			____card.ShowUpgradePreview();
		else
			____card.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
		state.CreateControls(__instance, ____leftButton, ____rightButton);
		Vector2 upgradeCenter = ____upgradeTickbox.Position + ____upgradeTickbox.Size / 2f;
		float arrowOffset = ____upgradeTickbox.Size.X / 2f + 56f + state.Left!.Size.X * state.Left.Scale.X / 2f;
		state.Left.Position = upgradeCenter + new Vector2(-arrowOffset, 0f) - state.Left.Size / 2f;
		state.Right!.Position = upgradeCenter + new Vector2(arrowOffset, 0f) - state.Right.Size / 2f;
		if (state.Rank > 1)
			state.Left.Enable();
		else
			state.Left.Disable();
		if (state.Rank < 3)
			state.Right.Enable();
		else
			state.Right.Disable();
		state.Left.Modulate = new Color(1f, 1f, 1f, state.Rank > 1 ? 1f : 0.35f);
		state.Right.Modulate = new Color(1f, 1f, 1f, state.Rank < 3 ? 1f : 0.35f);
		state.Left.Show();
		state.Right.Show();
		NHoverTipSet.Clear();
		NHoverTipSet.CreateAndShow(__instance, IHoverTip.RemoveDupes(preview.HoverTips.Append(LaffeyQuality.IncreaseHoverTip)))
			?.SetAlignment(____hoverTipRect, HoverTip.GetHoverTipAlignment(__instance));
	}
}
