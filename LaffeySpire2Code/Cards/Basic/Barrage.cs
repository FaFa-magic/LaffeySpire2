using LaffeySpire2.LaffeySpire2Code.Characters;
using LaffeySpire2.LaffeySpire2Code.Tags;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Basic;

[RegisterCharacterStarterCard(typeof(LaffeyCharacter), 1, Order = 2)]
public sealed class Barrage() : LaffeyCardModel(0, CardType.Attack, CardRarity.Basic, TargetType.RandomEnemy)
{
	public override int MaxUpgradeLevel => int.MaxValue;
	protected override HashSet<CardTag> CanonicalTags => [LaffeyTags.Barrage];
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(2M, ValueProp.Move),
		new RepeatVar(3)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(CombatState);
		
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.WithHitCount(DynamicVars.Repeat.IntValue)
			.FromCard(this, cardPlay)
			.TargetingRandomOpponents(CombatState)
			.WithHitFx("vfx/vfx_attack_blunt")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1M);
}
