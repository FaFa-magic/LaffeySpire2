using LaffeySpire2.LaffeySpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Common;

public sealed class DrowsyStrike() : LaffeyCardModel(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(10M, ValueProp.Move),
		new DynamicVar("Cards", 1M)
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromPower<DeepSleepPower>()];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
		await PowerCmd.Apply<DeepSleepPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars["Cards"].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4M);
}
