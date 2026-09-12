using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.Powers;

public sealed class SolomonsWarGodPower : LaffeyPowerModel
{
    private bool _isTakingGrantedExtraTurn;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://JanusSpire2/images/powers/big/AngelPrayerPower.png",
        BigIconPath: "res://JanusSpire2/images/powers/big/AngelPrayerPower.png"
    );

    [SavedProperty]
    public bool IsTakingGrantedExtraTurn
    {
        get => _isTakingGrantedExtraTurn;
        private set
        {
            AssertMutable();
            _isTakingGrantedExtraTurn = value;
        }
    }

    public override bool ShouldTakeExtraTurn(Player player)
    {
        return player == Owner.Player && Amount > 0;
    }

    public override Task AfterTakingExtraTurn(Player player)
    {
        if (player == Owner.Player && Amount > 0)
        {
            Flash();
            IsTakingGrantedExtraTurn = true;
        }

        return Task.CompletedTask;
    }

    public override bool ShouldPlayerResetEnergy(Player player)
    {
        return player != Owner.Player || !IsTakingGrantedExtraTurn || Amount <= 0;
    }

    public override bool ShouldDraw(Player player, bool fromHandDraw)
    {
        return player != Owner.Player || !IsTakingGrantedExtraTurn || Amount <= 0 || !fromHandDraw;
    }

    public override bool ShouldClearBlock(Creature creature)
    {
        return creature != Owner || !IsTakingGrantedExtraTurn || Amount <= 0;
    }

    public override bool ShouldFlush(Player player)
    {
        return player != Owner.Player ||
               Amount <= 0 ||
               IsTakingGrantedExtraTurn && Amount <= 1;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !IsTakingGrantedExtraTurn || !participants.Contains(Owner))
        {
            return;
        }

        IsTakingGrantedExtraTurn = false;
        await PowerCmd.Decrement(this);
    }
}
