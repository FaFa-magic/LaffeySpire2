using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Rare;

public sealed class BedtimePreparations() : LaffeyCardModel(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		int count = CardPile.MaxCardsInHand - PileType.Hand.GetPile(Owner).Cards.Count;
		await CardPileCmd.Draw(choiceContext, count, Owner);
		PlayerCmd.EndTurn(Owner, canBackOut: false);
	}

	protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Ethereal);
}
