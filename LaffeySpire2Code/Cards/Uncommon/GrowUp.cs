using LaffeySpire2.LaffeySpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Uncommon;

public sealed class GrowUp() : LaffeyCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("GrowUp", 1M)];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromPower<GrowUpPower>(),
		HoverTipFactory.FromPower<StrengthPower>()
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<GrowUpPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars["GrowUp"].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
