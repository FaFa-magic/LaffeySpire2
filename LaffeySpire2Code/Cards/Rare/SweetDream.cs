using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaffeySpire2.LaffeySpire2Code.Cards.Rare;

public sealed class SweetDream() : LaffeyCardModel(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
	public override bool GainsBlock => true;

	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(25M, ValueProp.Move)];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		PowerModel[] debuffs = Owner.Creature.Powers
			.Where(power => power.TypeForCurrentAmount == PowerType.Debuff)
			.OrderByDescending(power => power is ITemporaryPower)
			.ToArray();
		foreach (PowerModel power in debuffs)
		{
			if (power.TypeForCurrentAmount != PowerType.Debuff)
			{
				continue;
			}

			int targetAmount = Math.Clamp(power.Amount, -1, 1);
			if (targetAmount == power.Amount)
			{
				continue;
			}

			await PowerCmd.ModifyAmount(
				choiceContext,
				power,
				targetAmount - power.Amount,
				Owner.Creature,
				this);
		}
	}

	protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(8M);
}
