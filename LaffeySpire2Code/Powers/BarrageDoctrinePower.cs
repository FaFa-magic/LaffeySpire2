using LaffeySpire2.LaffeySpire2Code.Tags;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace LaffeySpire2.LaffeySpire2Code.Powers;

public sealed class BarrageDoctrinePower : LaffeyPowerModel
{
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.Static(StaticHoverTip.ReplayStatic)];

	public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
		decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (power == this)
			foreach (CardModel card in Owner.Player?.PlayerCombatState?.AllCards ?? [])
				ModifyReplays(card, (int)amount);
		return Task.CompletedTask;
	}

	public override Task AfterCardEnteredCombat(CardModel card)
	{
		if (!card.IsClone)
			ModifyReplays(card, Amount);
		return Task.CompletedTask;
	}

	public override Task AfterRemoved(Creature oldOwner)
	{
		foreach (CardModel card in oldOwner.Player?.PlayerCombatState?.AllCards ?? [])
			if (card.Owner == oldOwner.Player && card.Tags.Contains(LaffeyTags.Barrage))
				card.BaseReplayCount -= Amount;
		return Task.CompletedTask;
	}

	public void OnBarrageDesignated(CardModel card) => ModifyReplays(card, Amount);

	private void ModifyReplays(CardModel card, int amount)
	{
		if (card.Owner == Owner.Player && card.Tags.Contains(LaffeyTags.Barrage))
			card.BaseReplayCount += amount;
	}
}
