using LaffeySpire2.LaffeySpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Uncommon;

public sealed class NightWatchBarrage() : LaffeyCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
		[new PowerVar<NightWatchBarragePower>(1M)];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.Static(MegaCrit.Sts2.Core.HoverTips.StaticHoverTip.Block)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<NightWatchBarragePower>(
			choiceContext,
			Owner.Creature,
			DynamicVars["NightWatchBarragePower"].BaseValue,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
