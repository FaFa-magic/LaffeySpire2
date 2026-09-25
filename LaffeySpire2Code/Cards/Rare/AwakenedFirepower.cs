using LaffeySpire2.LaffeySpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Rare;

public sealed class AwakenedFirepower() : LaffeyCardModel(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

	protected override IEnumerable<DynamicVar> CanonicalVars =>
		[new PowerVar<AwakenedFirepowerPower>(1M)];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<AwakenedFirepowerPower>(),
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<AwakenedFirepowerPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars["AwakenedFirepowerPower"].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Ethereal);
}
