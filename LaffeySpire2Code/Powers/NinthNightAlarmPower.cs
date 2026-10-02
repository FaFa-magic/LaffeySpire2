using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace LaffeySpire2.LaffeySpire2Code.Powers;

public sealed class NinthNightAlarmPower : LaffeyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldTakeExtraTurn(Player player)
    {
        return player == Owner.Player && Amount > 0;
    }

    public override async Task AfterTakingExtraTurn(Player player)
    {
        if (player != Owner.Player || Amount <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Decrement(this);
    }
}
