using System.Threading;
using Godot;
using LaffeySpire2.LaffeySpire2Code.Characters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.CardQuality;

public sealed class QualityRestSiteOption(Player owner) : ModRestSiteOptionTemplate(owner)
{
	private CardModel[] _selection = [];
	public override string OptionId => "LAFFEY_REFIT";
	public override RestSiteOptionAssetProfile AssetProfile => new(
		IconPath: ImageHelper.GetImagePath("ui/rest_site/option_smith.png"));
	public override IEnumerable<string> AssetPaths => base.AssetPaths.Concat(NCardSmithVfx.AssetPaths);
	public override bool IsEnabled => Owner.Deck.Cards.Any(LaffeyQuality.CanIncrease);
	public override LocString Description => new("rest_site_ui",
		$"OPTION_{OptionId}.{(IsEnabled ? "description" : "descriptionDisabled")}");

	public override async Task<bool> OnSelect()
	{
		CardSelectorPrefs prefs = new(new LocString("rest_site_ui", "OPTION_LAFFEY_REFIT.prompt"), 1)
		{
			Cancelable = true,
			RequireManualConfirmation = true
		};
		_selection = (await CardSelectCmd.FromDeckGeneric(Owner, prefs, LaffeyQuality.CanIncrease)).ToArray();
		bool changed = false;
		foreach (CardModel card in _selection)
			changed |= LaffeyQuality.TryIncreasePermanently(card);
		return changed;
	}

	public override async Task DoLocalPostSelectVfx(CancellationToken ct = default)
	{
		NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(NCardSmithVfx.Create(_selection));
		await Cmd.CustomScaledWait(1f, 2f, ignoreCombatEnd: false, ct);
	}

	public override Task DoRemotePostSelectVfx()
	{
		NCardSmithVfx? vfx = NCardSmithVfx.Create();
		if (vfx != null)
		{
			NRestSiteRoom.Instance?.Characters.FirstOrDefault(character => character.Player == Owner)?.AddChildSafely(vfx);
			vfx.Position = Vector2.Zero;
		}
		return Task.CompletedTask;
	}
}

[RegisterSingleton]
public sealed class QualityRestSiteHook() : HookedSingletonModel(HookType.Run)
{
	public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
	{
		if (player.Character is not LaffeyCharacter || options.Any(option => option is QualityRestSiteOption))
			return false;
		options.Add(new QualityRestSiteOption(player));
		return true;
	}
}
