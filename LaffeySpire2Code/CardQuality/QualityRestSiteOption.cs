using System.Threading;
using Godot;
using LaffeySpire2.LaffeySpire2Code.Cards;
using LaffeySpire2.LaffeySpire2Code.Characters;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.CardQuality;

public sealed class QualityRestSiteOption(Player owner) : ModRestSiteOptionTemplate(owner)
{
	private CardModel[] _selection = [];
	public override string OptionId => "LAFFEY_REFIT";
	public override RestSiteOptionAssetProfile AssetProfile => new(
		IconPath: "res://LaffeySpire2/images/rest_site/option_laffey_refit.png");
	public override IEnumerable<string> AssetPaths => base.AssetPaths.Concat(NCardSmithVfx.AssetPaths);
	public override bool IsEnabled => Owner.Deck.Cards.Any(CanRefit);
	public override LocString Description => new("rest_site_ui",
		$"OPTION_{OptionId}.{(IsEnabled ? "description" : "descriptionDisabled")}");

	public override async Task<bool> OnSelect()
	{
		CardSelectorPrefs prefs = new(new LocString("rest_site_ui", "OPTION_LAFFEY_REFIT.prompt"), 1)
		{
			Cancelable = true,
			RequireManualConfirmation = true
		};
		_selection = (await SelectCardWithQualityPreview(prefs)).ToArray();
		bool changed = false;
		foreach (CardModel card in _selection)
			changed |= LaffeyQuality.TryIncreasePermanently(card);
		return changed;
	}

	private static bool CanRefit(CardModel card) => card.Type != CardType.Quest &&
		card.IsTransformable && LaffeyQuality.CanIncrease(card);

	private static CardTransformation CreateQualityPreview(CardModel card)
	{
		LaffeyCardModel preview = (LaffeyCardModel)card.MutableClone();
		preview.QualityRank = LaffeyQuality.RankForRarity(card.Rarity) + 1;
		return new CardTransformation(card, preview);
	}

	private async Task<IEnumerable<CardModel>> SelectCardWithQualityPreview(CardSelectorPrefs prefs)
	{
		List<CardModel> cards = Owner.Deck.Cards.Where(CanRefit).ToList();
		if (Owner.Creature.IsDead || cards.Count == 0)
			return [];

		if (!prefs.RequireManualConfirmation && cards.Count <= prefs.MinSelect)
			return cards;

		if (CardSelectCmd.Selector is not null)
			return await CardSelectCmd.Selector.GetSelectedCards(cards, prefs.MinSelect, prefs.MaxSelect);

		uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(Owner);
		bool selectsLocally = LocalContext.IsMe(Owner) &&
			RunManager.Instance.NetService.Type != NetGameType.Replay;
		if (!selectsLocally)
			return (await RunManager.Instance.PlayerChoiceSynchronizer
				.WaitForRemoteChoice(Owner, choiceId)).AsDeckCards();

		IEnumerable<CardModel> selectedCards;
		if (CardSelectCmd.LocalSelector is not null)
			selectedCards = await CardSelectCmd.LocalSelector.GetSelectedCards(cards, prefs.MinSelect, prefs.MaxSelect);
		else
		{
			NDeckTransformSelectScreen screen = NDeckTransformSelectScreen.ShowScreen(cards, CreateQualityPreview, prefs);
			selectedCards = await screen.CardsSelected();
		}

		List<CardModel> result = selectedCards.ToList();
		RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
			Owner, choiceId, PlayerChoiceResult.FromMutableDeckCards(result));
		return result;
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
