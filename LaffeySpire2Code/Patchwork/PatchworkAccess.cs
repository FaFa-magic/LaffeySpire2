using MegaCrit.Sts2.Core.Combat;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public static class PatchworkAccess
{
	// A finished combat still owns a CombatRoom while its reward screen is open.
	public static bool CanUse => !CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsStarting;
}
