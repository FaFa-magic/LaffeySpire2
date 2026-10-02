using LaffeySpire2.LaffeySpire2Code.Tags;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace LaffeySpire2.LaffeySpire2Code.Powers;

public sealed class BarrageRecallPower : LaffeyPowerModel
{
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Single;

	public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
		IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (!participants.Contains(Owner))
			return;
		var player = Owner.Player;
		if (player is null)
			return;
		CardModel[] cards =
		[
			.. new[] { PileType.Draw, PileType.Discard, PileType.Exhaust }
				.SelectMany(pile => pile.GetPile(player).Cards)
				.Where(card => card.Tags.Contains(LaffeyTags.Barrage))
		];
		Flash();
		await CardPileCmd.Add(cards, PileType.Hand);
		await PowerCmd.Remove(this);
	}
}
