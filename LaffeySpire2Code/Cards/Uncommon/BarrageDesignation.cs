using LaffeySpire2.LaffeySpire2Code.Powers;
using LaffeySpire2.LaffeySpire2Code.Tags;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Models.Capabilities;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Uncommon;

public sealed class BarrageDesignation() : LaffeyCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips => IsUpgraded
		? [HoverTipFactory.FromKeyword(CardKeyword.Exhaust), HoverTipFactory.FromKeyword(CardKeyword.Retain)]
		: [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		CardModel? selected = (await CardSelectCmd.FromHand(choiceContext, Owner,
			new CardSelectorPrefs(SelectionScreenPrompt, 1),
			card => card.Type == CardType.Attack && !card.Tags.Contains(LaffeyTags.Barrage), this)).FirstOrDefault();
		if (selected == null)
			return;
		selected.GetOrCreateCapability<BarrageDesignationCapability>();
		Owner.Creature.GetPower<BarrageDoctrinePower>()?.OnBarrageDesignated(selected);
	}

	protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
