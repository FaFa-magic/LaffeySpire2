using LaffeySpire2.LaffeySpire2Code.Keywords;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace LaffeySpire2.LaffeySpire2Code.Powers;

public sealed class WarGodFormPower : LaffeyPowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;
	
	public override bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords)
	{
		return card.Owner == Owner.Player && keywords.Add(LaffeyKeywords.Opening);
	}
}
