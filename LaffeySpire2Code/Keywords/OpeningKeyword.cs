using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace LaffeySpire2.LaffeySpire2Code.Keywords;

[RegisterSingleton]
public sealed class OpeningKeyword : HookedSingletonModel
{
    public OpeningKeyword() : base(HookType.Combat)
    {
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (!card.Keywords.Contains(LaffeyKeywords.Opening))
        {
            return playCount;
        }

        bool hasPlayedCardThisTurn = CombatManager.Instance.History.CardPlaysStarted.Any(entry =>
            entry.CardPlay.Player == card.Owner &&
            entry.CardPlay.IsFirstInSeries &&
            entry.HappenedThisTurn(card.CombatState));

        return hasPlayedCardThisTurn ? playCount : playCount + 1;
    }
}
