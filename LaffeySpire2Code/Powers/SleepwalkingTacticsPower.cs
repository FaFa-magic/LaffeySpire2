using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.Powers;

public sealed class SleepwalkingTacticsPower : LaffeyPowerModel
{
	private sealed class Data
	{
		public int CardsPlayedThisTurn;
	}

	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter;
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://JanusSpire2/images/powers/big/AngelPrayerPower.png",
		BigIconPath: "res://JanusSpire2/images/powers/big/AngelPrayerPower.png"
	);

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.ForEnergy(this)];

	protected override object InitInternalData() => new Data();

	public override async Task AfterEnergyReset(Player player)
	{
		if (player == Owner.Player)
			await PlayerCmd.LoseEnergy(Amount, player);
	}

	public override Task BeforeSideTurnStart(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IReadOnlyList<Creature> participants,
		ICombatState combatState)
	{
		if (participants.Contains(Owner))
			GetInternalData<Data>().CardsPlayedThisTurn = 0;
		return Task.CompletedTask;
	}

	public override bool TryModifyEnergyCostInCombatLate(
		CardModel card,
		decimal originalCost,
		out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!CanPlayForFree(card))
			return false;
		modifiedCost = 0M;
		return true;
	}

	public override bool TryModifyStarCost(
		CardModel card,
		decimal originalCost,
		out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!CanPlayForFree(card))
			return false;
		modifiedCost = 0M;
		return true;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature == Owner && !cardPlay.IsAutoPlay && cardPlay.IsLastInSeries)
			GetInternalData<Data>().CardsPlayedThisTurn++;
		return Task.CompletedTask;
	}

	private bool CanPlayForFree(CardModel card) =>
		card.Owner.Creature == Owner &&
		(card.Pile?.Type is PileType.Hand or PileType.Play) &&
		GetInternalData<Data>().CardsPlayedThisTurn < Amount;
}
