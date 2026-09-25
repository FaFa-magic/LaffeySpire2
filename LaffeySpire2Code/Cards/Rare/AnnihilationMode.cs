using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Rare;

public sealed class AnnihilationMode() : LaffeyCardModel(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(4M, ValueProp.Move),
		new RepeatVar(3)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.WithHitCount(DynamicVars.Repeat.IntValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_blunt")
			.Execute(choiceContext);
	}

	public override async Task AfterAutoPrePlayPhaseEnteredEarly(PlayerChoiceContext choiceContext, Player player)
	{
		if (player == Owner && Pile?.Type == PileType.Exhaust)
			await CardCmd.AutoPlay(choiceContext, this, null);
	}

	protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1M);
}
