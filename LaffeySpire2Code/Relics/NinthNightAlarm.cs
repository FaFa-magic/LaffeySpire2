using LaffeySpire2.LaffeySpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace LaffeySpire2.LaffeySpire2Code.Relics;

public sealed class NinthNightAlarm : LaffeyRelicModel
{
    private int _turnsSeen;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShowCounter => true;

    public override int DisplayAmount => TurnsSeen;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Turns", 9M)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<NinthNightAlarmPower>()];

    [SavedProperty]
    public int TurnsSeen
    {
        get => _turnsSeen;
        set
        {
            AssertMutable();
            _turnsSeen = value;
            Status = CombatManager.Instance.IsInProgress &&
                     TurnsSeen == DynamicVars["Turns"].IntValue - 1
                ? RelicStatus.Active
                : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner.Creature))
        {
            return;
        }

        TurnsSeen = (TurnsSeen + 1) % DynamicVars["Turns"].IntValue;
        if (TurnsSeen != 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<NinthNightAlarmPower>(
            choiceContext,
            Owner.Creature,
            1M,
            Owner.Creature,
            null);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }
}
