using LaffeySpire2.LaffeySpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Rare;

public sealed class Solomon() : LaffeyCardModel(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	protected override IEnumerable<DynamicVar> CanonicalVars =>
		[new PowerVar<SolomonsWarGodPower>(2M)];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromPower<SolomonsWarGodPower>()];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<SolomonsWarGodPower>(
			choiceContext,
			Owner.Creature,
			DynamicVars["SolomonsWarGodPower"].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade() =>
		DynamicVars["SolomonsWarGodPower"].UpgradeValueBy(1M);
}
