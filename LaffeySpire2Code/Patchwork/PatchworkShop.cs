using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rooms;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public static class PatchworkShop
{
	private static readonly ConditionalWeakTable<MerchantInventory, PatchworkMerchantEntry> Entries = new();
	public static PatchworkMerchantEntry? GetEntry(MerchantInventory inventory) =>
		inventory.Player.Character is Characters.LaffeyCharacter && inventory.Player.RunState.CurrentRoom is MerchantRoom
			? Entries.GetValue(inventory, i => new PatchworkMerchantEntry(i.Player)) : null;
	public static PatchworkMerchantSlot? FindSlot(NMerchantInventory rug) =>
		rug.GetNodeOrNull<PatchworkMerchantSlot>("%SlotsContainer/PatchworkChip");
}
