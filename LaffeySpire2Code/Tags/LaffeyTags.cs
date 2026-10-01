using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.CardTags;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;

namespace LaffeySpire2.LaffeySpire2Code.Tags;

[RegisterOwnedCardTag(nameof(Barrage))]
public sealed class LaffeyTags
{
	public static readonly CardTag Barrage = ModContentRegistry
		.GetQualifiedCardTagId(MainFile.ModId, nameof(Barrage)).GetModCardTag();
}
