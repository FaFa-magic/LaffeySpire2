using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public sealed class PatchworkMerchantEntry : MerchantEntry
{
	public const int BasePrice = 50;
	public string ShopKey { get; }
	private int? _synchronizedCost;
	public override bool IsStocked => _player.RunState.CurrentRoom is MerchantRoom &&
		ShopKey == PatchworkActions.ShopKey(_player) &&
		!PatchworkBoard.Get(_player).PurchasedShops.Contains(ShopKey) &&
		PatchworkBoard.HasSpecialStock(PatchworkBoard.Get(_player));
	public PatchworkMerchantEntry(Player player) : base(player)
	{
		ShopKey = PatchworkActions.ShopKey(player);
		CalcCost();
	}
	public override void CalcCost() => _cost = BasePrice;
	// Some price hooks depend on LocalContext. The owner quotes the price through
	// vanilla PlayerChoiceSynchronizer so every managed-action replica charges the same amount.
	public async Task<bool> PurchaseSynchronized(MerchantInventory inventory, int cost)
	{
		if (!IsStocked || inventory.Player != _player)
		{
			InvokePurchaseFailed(PurchaseStatus.FailureOutOfStock);
			return false;
		}
		if (cost < 0 || _player.Gold < cost)
		{
			InvokePurchaseFailed(PurchaseStatus.FailureGold);
			return false;
		}
		_synchronizedCost = cost;
		try
		{
			// Skip the peer-local affordability check, then charge the agreed cost below.
			// Retain official AfterItemPurchased and purchase feedback events.
			return await OnTryPurchaseWrapper(inventory, ignoreCost: true);
		}
		finally { _synchronizedCost = null; }
	}
	protected override async Task<(bool, int)> OnTryPurchase(MerchantInventory? inventory, bool ignoreCost)
	{
		if (_synchronizedCost is not { } cost || !IsStocked || inventory?.Player != _player) return (false, 0);
		// Reserve before any awaited command to reject duplicate queued requests.
		PatchworkBoard.Modify(_player, state =>
		{
			state.PurchasedShops.Add(ShopKey);
			state.AvailablePieces.Add(0);
		});
		await PlayerCmd.LoseGold(cost, _player, GoldLossType.Spent);
		// The managed action already runs on all peers; also SyncLocalGoldLost would double-charge.
		return (true, cost);
	}
	protected override void ClearAfterPurchase() { }
	// Preserve the finite one-per-shop / five-total stock, even with Courier.
	protected override void RestockAfterPurchase(MerchantInventory? inventory) { }
}
