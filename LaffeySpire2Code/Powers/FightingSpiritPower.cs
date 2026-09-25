using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.Powers;

public sealed class FightingSpiritPower : LaffeyPowerModel
{
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter;
	public override int DisplayAmount => DynamicVars["Bonus"].IntValue;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://JanusSpire2/images/powers/big/AngelPrayerPower.png",
		BigIconPath: "res://JanusSpire2/images/powers/big/AngelPrayerPower.png"
	);

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Bonus", 0M),
		new DynamicVar("Growth", 10M)
	];

	public void GainDamageBonus(decimal amount)
	{
		DynamicVars["Bonus"].BaseValue += amount;
		InvokeDisplayAmountChanged();
	}

	public override Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		if (power == this)
			DynamicVars["Growth"].BaseValue = Amount * 10M;
		return Task.CompletedTask;
	}

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player == Owner.Player)
		{
			Flash();
			GainDamageBonus(DynamicVars["Growth"].BaseValue);
		}
		return Task.CompletedTask;
	}

	public override decimal ModifyDamageMultiplicative(
		Creature? target,
		decimal amount,
		ValueProp props,
		Creature? dealer,
		CardModel? cardSource,
		CardPlay? cardPlay)
	{
		if (dealer != Owner || target == Owner)
			return 1M;
		return 1M + DynamicVars["Bonus"].BaseValue / 100M;
	}
}
