using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public static class PatchworkAccess
{
	// A finished combat still owns a CombatRoom while its reward screen is open.
	public static bool CanUse => !CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsStarting;
	public static bool CanUseFor(Player player) => CanUse && !PatchworkActions.IsChoosingAncient(player);
}
