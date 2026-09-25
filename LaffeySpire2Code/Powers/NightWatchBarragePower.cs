using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.Powers;

public sealed class NightWatchBarragePower : LaffeyPowerModel
{
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://JanusSpire2/images/powers/big/AngelPrayerPower.png",
		BigIconPath: "res://JanusSpire2/images/powers/big/AngelPrayerPower.png"
	);

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.Static(StaticHoverTip.Block)];

	public override async Task BeforeSideTurnEnd(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IEnumerable<Creature> participants)
	{
		if (!participants.Contains(Owner))
			return;

		int hits = Amount;
		decimal block = Owner.Block;
		Flash();
		if (block > 0M)
		{
			for (int i = 0; i < hits; i++)
				await CreatureCmd.Damage(choiceContext, CombatState.HittableEnemies, block, ValueProp.Unpowered, Owner);
		}
		await PowerCmd.Remove(this);
	}
}
