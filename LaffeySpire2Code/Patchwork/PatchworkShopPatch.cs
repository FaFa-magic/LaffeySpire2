using Godot;
using HarmonyLib;
using LaffeySpire2.LaffeySpire2Code.Characters;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using STS2RitsuLib.Patching.Models;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public sealed class PatchworkShopPatch : IPatchMethod
{
	public static string PatchId => "laffey_patchwork_shop_slot";
	public static string Description => "Add a 50 gold one-cell patch to Laffey merchant inventory";
	public static bool IsCritical => true;

	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize),
			[typeof(MerchantInventory), typeof(MerchantDialogueSet)])
	];

	[HarmonyPostfix]
	public static void Postfix(NMerchantInventory __instance, MerchantInventory inventory)
	{
		if (inventory.Player.Character is not LaffeyCharacter ||
			inventory.Player.RunState.CurrentRoom is not MegaCrit.Sts2.Core.Rooms.MerchantRoom)
			return;
		Control slots = __instance.GetNode<Control>("%SlotsContainer");
		PatchworkShopButton button = new()
		{
			Position = new Vector2(1410, 900),
			CustomMinimumSize = new Vector2(235, 64),
			Size = new Vector2(235, 64)
		};
		button.Initialize(inventory.Player);
		slots.AddChild(button);
	}
}

public sealed partial class PatchworkShopButton : Button
{
	private Player _player = null!;

	public void Initialize(Player player)
	{
		_player = player;
		Pressed += Purchase;
	}

	public override void _Process(double delta)
	{
		PatchworkSaveData state = PatchworkBoard.Get(_player);
		bool sold = state.PurchasedShops.Contains(PatchworkActions.ShopKey(_player)) ||
			!PatchworkBoard.HasSpecialStock(state);
		Disabled = sold || _player.Gold < 50;
		Text = sold ? "1×1 拼图：已售罄" : "1×1 拼图  50 金币";
	}

	private void Purchase()
	{
		if (!Disabled && PatchworkActions.RequestShopPurchase(_player))
			Disabled = true;
	}
}
