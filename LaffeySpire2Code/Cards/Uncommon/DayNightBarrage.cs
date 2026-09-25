using LaffeySpire2.LaffeySpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Uncommon;

public sealed class DayNightBarrage() : LaffeyCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
		[new PowerVar<DayNightBarragePower>(2M)];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromPower<DayNightBarragePower>()];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<DayNightBarragePower>(
			choiceContext,
			Owner.Creature,
			DynamicVars["DayNightBarragePower"].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade() => DynamicVars["DayNightBarragePower"].UpgradeValueBy(1M);
}
