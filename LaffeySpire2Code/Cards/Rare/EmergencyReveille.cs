using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Rare;

public sealed class EmergencyReveille() : LaffeyCardModel(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		CardModel[] cards = PileType.Hand.GetPile(Owner).Cards.ToArray();
		foreach (CardModel card in cards)
		{
			if (card.Pile?.Type == PileType.Hand)
				await CardCmd.AutoPlay(choiceContext, card, null);
		}
		PlayerCmd.EndTurn(Owner, canBackOut: false);
	}

	protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
