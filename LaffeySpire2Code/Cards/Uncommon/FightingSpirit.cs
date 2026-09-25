using LaffeySpire2.LaffeySpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Uncommon;

public sealed class FightingSpirit() : LaffeyCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
		[new PowerVar<FightingSpiritPower>(1M)];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromPower<FightingSpiritPower>()];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		FightingSpiritPower? power = await PowerCmd.Apply<FightingSpiritPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars["FightingSpiritPower"].BaseValue,
			Owner.Creature,
			this);
		if (IsUpgraded)
			power?.GainDamageBonus(10M);
	}
}
