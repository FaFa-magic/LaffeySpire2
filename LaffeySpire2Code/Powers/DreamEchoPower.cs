using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace LaffeySpire2.LaffeySpire2Code.Powers;

public sealed class DreamEchoPower : LaffeyPowerModel
{
	private sealed class Data
	{
		public CardModel? SelectedCard;
	}

	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter;
	public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("Card")];

	protected override object InitInternalData() => new Data();

	public void SetSelectedCard(CardModel card)
	{
		CardModel template = card.CreateClone();
		CardCmd.ClearAffliction(template);
		GetInternalData<Data>().SelectedCard = template;
		((StringVar)DynamicVars["Card"]).StringValue = template.Title;
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Combat.ICombatState combatState)
	{
		if (player != Owner.Player || GetInternalData<Data>().SelectedCard is not { } template)
			return;

		CardModel copy = template.CreateClone();
		copy.ExhaustOnNextPlay = true;
		Flash();
		await CardCmd.AutoPlay(choiceContext, copy, null);
		await PowerCmd.Decrement(this);
	}
}
