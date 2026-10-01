using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.Powers;

public sealed class SleepwalkingTacticsPower : LaffeyPowerModel
{
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter;
	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://JanusSpire2/images/powers/big/AngelPrayerPower.png",
		BigIconPath: "res://JanusSpire2/images/powers/big/AngelPrayerPower.png"
	);

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.ForEnergy(this)];

	public override async Task AfterEnergyReset(Player player)
	{
		if (player == Owner.Player)
			await PlayerCmd.LoseEnergy(Amount, player);
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

	private bool CanPlayForFree(CardModel card) =>
		card.Owner.Creature == Owner &&
		(card.Pile?.Type is PileType.Hand or PileType.Play) &&
		!CombatManager.Instance.History.CardPlaysStarted.Any((CardPlayStartedEntry entry) =>
			entry.Actor == Owner && entry.HappenedThisTurn(CombatState) &&
			!entry.CardPlay.IsAutoPlay && entry.CardPlay.IsFirstInSeries);
}
