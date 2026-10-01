using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace LaffeySpire2.LaffeySpire2Code.Tags;

[RegisterModelCapability]
public sealed class BarrageDesignationCapability : ModelCapability, ICardTitleContributor, ICardPropertyContributor
{
	public IEnumerable<CardTitleFragment> GetTitleFragments(CardTitleContext context) =>
		[new(new LocString("cards", "LAFFEY_SPIRE2_CARD_BARRAGE.title"), CardTitleFragmentPlacement.ReplaceBase)];

	public IEnumerable<CardTag> GetTags(CardModel card) => [LaffeyTags.Barrage];
}
