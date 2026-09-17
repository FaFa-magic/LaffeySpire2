using LaffeySpire2.LaffeySpire2Code.Keywords;
using LaffeySpire2.LaffeySpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Rare;

public sealed class WarGodForm() : LaffeyCardModel(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		[HoverTipFactory.FromKeyword(LaffeyKeywords.Opening)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<WarGodFormPower>(
			choiceContext,
			Owner.Creature,
			1M,
			Owner.Creature,
			this);
	}

	protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
