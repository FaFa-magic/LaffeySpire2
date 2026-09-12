using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace LaffeySpire2.LaffeySpire2Code.Keywords;

[RegisterOwnedCardKeyword(nameof(Opening), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
public sealed class LaffeyKeywords
{
    public static readonly CardKeyword Opening = ModContentRegistry
        .GetQualifiedKeywordId(MainFile.ModId, nameof(Opening))
        .GetModCardKeyword();
}
