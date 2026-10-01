using LaffeySpire2.LaffeySpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Common;

public sealed class BarrageRecall() : LaffeyCardModel(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromPower<BarrageRecallPower>()];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<BarrageRecallPower>(choiceContext, Owner.Creature, 1M, Owner.Creature, this);
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
	}

	protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1M);
}
