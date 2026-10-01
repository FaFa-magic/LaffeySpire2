using LaffeySpire2.LaffeySpire2Code.CardQuality;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Common;

public sealed class RiggingRefit() : LaffeyCardModel(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5M, ValueProp.Move)];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips => [LaffeyQuality.IncreaseHoverTip];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		IEnumerable<CardModel> cards = IsUpgraded
			? PileType.Hand.GetPile(Owner).Cards.Where(LaffeyQuality.CanIncrease).ToArray()
			: await CardSelectCmd.FromHand(choiceContext, Owner,
				new CardSelectorPrefs(SelectionScreenPrompt, 1), LaffeyQuality.CanIncrease, this);
		foreach (CardModel card in cards)
			LaffeyQuality.TryIncreaseInCombat(card);
	}
}
