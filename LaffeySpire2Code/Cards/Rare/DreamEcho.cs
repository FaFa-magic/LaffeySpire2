using LaffeySpire2.LaffeySpire2Code.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Rare;

public sealed class DreamEcho() : LaffeyCardModel(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Turns", 3M)];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		IEnumerable<CardModel> cards = await CardSelectCmd.FromHand(
			choiceContext,
			Owner,
			new CardSelectorPrefs(SelectionScreenPrompt, 1),
			null,
			this);
		CardModel? selected = cards.FirstOrDefault();
		if (selected == null)
			return;

		DreamEchoPower? power = await PowerCmd.Apply<DreamEchoPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars["Turns"].BaseValue,
			Owner.Creature,
			this);
		if (power == null)
			return;

		power.SetSelectedCard(selected);
		await CardCmd.Exhaust(choiceContext, selected);
	}

	protected override void OnUpgrade() => DynamicVars["Turns"].UpgradeValueBy(1M);
}
