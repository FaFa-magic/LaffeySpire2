using LaffeySpire2.LaffeySpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Rare;

public sealed class BarrageDoctrine() : LaffeyCardModel(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BarrageDoctrinePower>(1M)];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromPower<BarrageDoctrinePower>(), HoverTipFactory.Static(StaticHoverTip.ReplayStatic)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<BarrageDoctrinePower>(choiceContext, Owner.Creature,
			DynamicVars["BarrageDoctrinePower"].BaseValue, Owner.Creature, this);
	}

	protected override void OnUpgrade() => DynamicVars["BarrageDoctrinePower"].UpgradeValueBy(1M);
}
