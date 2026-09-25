using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.Powers;

public sealed class DayNightBarragePower : LaffeyPowerModel
{
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://JanusSpire2/images/powers/big/AngelPrayerPower.png",
		BigIconPath: "res://JanusSpire2/images/powers/big/AngelPrayerPower.png"
	);

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner.Player)
			return;
		Flash();
		await CreatureCmd.Damage(choiceContext, CombatState.HittableEnemies, Amount, ValueProp.Unpowered, Owner);
	}

	public override async Task BeforeSideTurnEnd(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (!participants.Contains(Owner))
			return;
		Flash();
		await CreatureCmd.Damage(choiceContext, CombatState.HittableEnemies, Amount, ValueProp.Unpowered, Owner);
	}
}
