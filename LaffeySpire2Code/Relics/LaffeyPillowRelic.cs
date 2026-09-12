using LaffeySpire2.LaffeySpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace LaffeySpire2.LaffeySpire2Code.Relics;

public abstract class LaffeyPillowRelic : LaffeyRelicModel
{
    private int _cardsPlayedThisTurn;

    protected abstract int CardThreshold { get; }

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress && CardsPlayedThisTurn < CardThreshold;

    public override int DisplayAmount => CardsPlayedThisTurn;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(CardThreshold)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<SolomonsWarGodPower>()];

    [SavedProperty]
    public int CardsPlayedThisTurn
    {
        get => _cardsPlayedThisTurn;
        private set
        {
            AssertMutable();
            _cardsPlayedThisTurn = value;
            Status = CardsPlayedThisTurn == CardThreshold - 1 ? RelicStatus.Active : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature))
        {
            CardsPlayedThisTurn = 0;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!CombatManager.Instance.IsInProgress ||
            cardPlay.IsAutoPlay ||
            cardPlay.Card.Owner != Owner ||
            CardsPlayedThisTurn >= CardThreshold)
        {
            return;
        }

        CardsPlayedThisTurn++;
        if (CardsPlayedThisTurn != CardThreshold)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<SolomonsWarGodPower>(
            choiceContext,
            Owner.Creature,
            1M,
            Owner.Creature,
            null);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        CardsPlayedThisTurn = 0;
        return Task.CompletedTask;
    }
}
