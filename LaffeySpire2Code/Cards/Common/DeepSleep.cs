using LaffeySpire2.LaffeySpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Common;

public sealed class DeepSleep() : LaffeyCardModel(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(13M, ValueProp.Move),
		new DynamicVar("Cards", 1M)
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromPower<DeepSleepPower>()];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		await PowerCmd.Apply<DeepSleepPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars["Cards"].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3M);
}
