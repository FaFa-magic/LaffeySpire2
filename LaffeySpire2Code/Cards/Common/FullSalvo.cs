using LaffeySpire2.LaffeySpire2Code.Tags;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Common;

public sealed class FullSalvo() : LaffeyCardModel(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(CombatState);
		CardModel[] cards =
		[
			.. new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust }
				.SelectMany(pile => pile.GetPile(Owner).Cards)
				.Where(card => card.Tags.Contains(LaffeyTags.Barrage))
		];
		if (IsUpgraded)
			foreach (CardModel card in cards)
				CardCmd.Upgrade(card, CardPreviewStyle.None);
		foreach (CardModel card in cards)
		{
			if (CombatManager.Instance.IsOverOrEnding)
				break;
			Creature? target = card.TargetType == TargetType.AnyEnemy
				? Owner.RunState.Rng.CombatTargets.NextItem(CombatState.HittableEnemies)
				: null;
			await CardCmd.AutoPlay(choiceContext, card, target);
		}
	}
}
