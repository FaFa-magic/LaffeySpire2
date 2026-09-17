using LaffeySpire2.LaffeySpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Uncommon;

public sealed class TrappedInTheEnemy() : LaffeyCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VulnerablePower>(1M)];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<VulnerablePower>(),
		HoverTipFactory.FromPower<TrappedInTheEnemyPower>()
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<VulnerablePower>(
			choiceContext,
			Owner.Creature,
			DynamicVars.Vulnerable.BaseValue,
			Owner.Creature,
			this);
		await PowerCmd.Apply<VulnerablePower>(
			choiceContext,
			CombatState?.HittableEnemies,
			DynamicVars.Vulnerable.BaseValue,
			Owner.Creature,
			this);
		await PowerCmd.Apply<TrappedInTheEnemyPower>(
			choiceContext,
			Owner.Creature,
			1M,
			Owner.Creature,
			this);
	}
}
