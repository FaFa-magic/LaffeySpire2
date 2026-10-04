using LaffeySpire2.LaffeySpire2Code.CardQuality;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Rare;

public sealed class Refit() : LaffeyCardModel(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	protected override IEnumerable<DynamicVar> CanonicalVars =>
		[new CardsVar(2), new DynamicVar("QualityIncreases", 2M)];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips => [LaffeyQuality.IncreaseHoverTip];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		foreach (CardModel card in await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner))
		{
			for (int i = 0; i < DynamicVars["QualityIncreases"].IntValue; i++)
			{
				if (!LaffeyQuality.TryIncreaseInCombat(card))
					break;
			}
		}
	}

	protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1M);
}
