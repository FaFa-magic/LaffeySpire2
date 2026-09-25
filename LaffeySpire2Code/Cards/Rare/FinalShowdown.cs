using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Rare;

public sealed class FinalShowdown() : LaffeyCardModel(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
	protected override bool IsPlayable => Owner.PlayerCombatState?.TurnNumber >= 7;
	protected override bool ShouldGlowGoldInternal => IsPlayable;
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(42M, ValueProp.Move)];
	public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) => card != this || IsPlayable;

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_blunt", null, "heavy_attack.mp3")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(24M);
}
