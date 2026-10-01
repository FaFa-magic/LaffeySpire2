using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using STS2RitsuLib.Patching.Models;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public sealed class PatchworkShopPatch : IPatchMethod
{
	public static string PatchId => "laffey_patchwork_shop_slot";
	public static string Description => "Add a native merchant slot for the one-cell Laffey chip";
	public static bool IsCritical => true;

	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize),
			[typeof(MerchantInventory), typeof(MerchantDialogueSet)])
	];

	// Add before vanilla enumerates slots and connects controller focus tracking.
	[HarmonyPrefix]
	public static void Prefix(NMerchantInventory __instance, MerchantInventory inventory)
	{
		PatchworkMerchantEntry? entry = PatchworkShop.GetEntry(inventory);
		if (entry == null || PatchworkShop.FindSlot(__instance) != null) return;
		Control slots = __instance.GetNode<Control>("%SlotsContainer");
		PatchworkMerchantSlot slot = ResourceLoader.Load<PackedScene>(PatchworkMerchantSlot.ScenePath)
			.Instantiate<PatchworkMerchantSlot>();
		slot.SetEntry(entry);
		Control removal = __instance.GetNode<Control>("%MerchantCardRemoval");
		slot.Position = removal.Position + new Vector2(-35, 255);
		slots.AddChild(slot);
	}
	[HarmonyPostfix]
	public static void Postfix(NMerchantInventory __instance) =>
		PatchworkShop.FindSlot(__instance)?.Initialize(__instance);
}

public sealed class PatchworkShopEntriesPatch : IPatchMethod
{
	public static string PatchId => "laffey_patchwork_shop_entries";
	public static string Description => "Include chips in native merchant updates and purchase events";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() => [new(typeof(MerchantInventory), "get_AllEntries")];
	[HarmonyPostfix]
	public static void Postfix(MerchantInventory __instance, ref IEnumerable<MerchantEntry> __result)
	{
		if (PatchworkShop.GetEntry(__instance) is { } entry) __result = __result.Append(entry);
	}
}

public sealed class PatchworkShopSlotsPatch : IPatchMethod
{
	public static string PatchId => "laffey_patchwork_shop_slots";
	public static string Description => "Include chips in native merchant focus and purchase feedback";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() => [new(typeof(NMerchantInventory), nameof(NMerchantInventory.GetAllSlots))];
	[HarmonyPostfix]
	public static void Postfix(NMerchantInventory __instance, ref IEnumerable<NMerchantSlot> __result)
	{
		if (PatchworkShop.FindSlot(__instance) is { } slot) __result = __result.Append(slot);
	}
}

public sealed class PatchworkShopNavigationPatch : IPatchMethod
{
	public static string PatchId => "laffey_patchwork_shop_navigation";
	public static string Description => "Connect the extra chip slot to merchant controller navigation";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() => [new(typeof(NMerchantInventory), "UpdateNavigation")];
	[HarmonyPostfix]
	public static void Postfix(NMerchantInventory __instance)
	{
		if (PatchworkShop.FindSlot(__instance) is not { Visible: true } slot) return;
		var others = __instance.GetAllSlots().Where(s => s != slot && s.Visible && s.Entry.IsStocked).ToList();
		foreach (var (direction, opposite) in new[] { (Vector2.Left, Vector2.Right), (Vector2.Right, Vector2.Left),
			(Vector2.Up, Vector2.Down), (Vector2.Down, Vector2.Up) })
		{
			NMerchantSlot? nearest = others.Where(s => (s.GlobalPosition - slot.GlobalPosition).Dot(direction) > 1)
				.MinBy(s => (s.GlobalPosition - slot.GlobalPosition).LengthSquared());
			SetNeighbor(slot, direction, nearest ?? slot);
			if (nearest == null) continue;
			NodePath oldPath = GetNeighbor(nearest, opposite);
			Control? old = oldPath.IsEmpty ? null : nearest.GetNodeOrNull<Control>(oldPath);
			if (old == null || old == nearest || !old.Visible ||
				(slot.GlobalPosition - nearest.GlobalPosition).LengthSquared() < (old.GlobalPosition - nearest.GlobalPosition).LengthSquared())
				SetNeighbor(nearest, opposite, slot);
		}
	}
	private static NodePath GetNeighbor(Control node, Vector2 direction) => direction == Vector2.Left ? node.FocusNeighborLeft :
		direction == Vector2.Right ? node.FocusNeighborRight : direction == Vector2.Up ? node.FocusNeighborTop : node.FocusNeighborBottom;
	private static void SetNeighbor(Control node, Vector2 direction, Control target)
	{
		if (direction == Vector2.Left) node.FocusNeighborLeft = target.GetPath();
		else if (direction == Vector2.Right) node.FocusNeighborRight = target.GetPath();
		else if (direction == Vector2.Up) node.FocusNeighborTop = target.GetPath();
		else node.FocusNeighborBottom = target.GetPath();
	}
}
