using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public sealed partial class PatchworkMerchantSlot : NMerchantSlot
{
	public const string ScenePath = "res://LaffeySpire2/scenes/ui/patchwork_merchant_slot.tscn";
	private PatchworkMerchantEntry _entry = null!;
	private Control _chip = null!;
	private bool _pending;
	private double _refreshSeconds, _pendingSeconds;
	public override MerchantEntry Entry => _entry;
	protected override CanvasItem Visual => _chip;
	public void SetEntry(PatchworkMerchantEntry entry) => _entry = entry;
	public override void _Ready()
	{
		_chip = GetNode<Control>("ChipHolder");
		GetNode<TextureRect>("ChipHolder/Icon").Texture = PatchworkVisuals.Component(0);
		// Same native click target, price font, outline and gold icon as merchant relics.
		var hitbox = new NClickableControl { Name = "Hitbox", Position = new(-78, -86), Size = new(156, 210) };
		AddChild(hitbox);
		hitbox.Owner = this;
		hitbox.UniqueNameInOwner = true;
		var label = new MegaLabel { Name = "CostLabel", AutoSizeEnabled = false, MouseFilter = MouseFilterEnum.Ignore };
		label.AddThemeFontOverride("font", ResourceLoader.Load<Font>("res://themes/kreon_bold_glyph_space_two.tres"));
		label.AddThemeFontSizeOverride("font_size", 39);
		label.AddThemeConstantOverride("outline_size", 15);
		label.AddThemeColorOverride("font_outline_color", new Color(0.067f, 0, 0, 0.43f));
		GetNode<HBoxContainer>("Cost").AddChild(label);
		label.Owner = this;
		label.UniqueNameInOwner = true;
		ConnectSignals();
		_entry.EntryUpdated += UpdateVisual;
		_entry.PurchaseFailed += PurchaseFailed;
		_entry.PurchaseCompleted += PurchaseCompleted;
		UpdateVisual();
	}
	protected override void UpdateVisual()
	{
		Visible = _entry.IsStocked;
		MouseFilter = MouseFilterEnum.Ignore;
		if (!Visible)
		{
			_pending = false;
			ClearHoverTip();
			return;
		}
		base.UpdateVisual();
		_costLabel.Modulate = _entry.EnoughGold ? StsColors.cream : StsColors.red;
		_chip.Modulate = _pending ? new Color(1, 1, 1, 0.5f) : Colors.White;
	}
	public override void _Process(double delta)
	{
		_refreshSeconds += delta;
		if (_pending && (_pendingSeconds += delta) > 5) _pending = false;
		if (_refreshSeconds < 0.2) return;
		_refreshSeconds = 0;
		UpdateVisual();
	}
	protected override Task OnTryPurchase(MerchantInventory? inventory)
	{
		if (_pending || inventory == null) return Task.CompletedTask;
		if (!_entry.IsStocked) _entry.InvokePurchaseFailed(PurchaseStatus.FailureOutOfStock);
		else if (!_entry.EnoughGold) _entry.InvokePurchaseFailed(PurchaseStatus.FailureGold);
		else if (PatchworkActions.RequestShopPurchase(inventory.Player, _entry.ShopKey))
		{
			_pending = true;
			_pendingSeconds = 0;
			UpdateVisual();
		}
		return Task.CompletedTask;
	}
	private void PurchaseFailed(PurchaseStatus status)
	{
		_pending = false;
		OnPurchaseFailed(status);
		UpdateVisual();
	}
	private void PurchaseCompleted(PurchaseStatus status, MerchantEntry entry)
	{
		_pending = false;
		TriggerMerchantHandToPointHere();
		UpdateVisual();
	}
	protected override void CreateHoverTip()
	{
		if (_pending || !_entry.IsStocked || NGame.Instance == null) return;
		var tip = new HoverTip(PatchworkVisuals.LocalizedText("SHOP_TITLE"),
			PatchworkVisuals.LocalizedText("SHOP_DESCRIPTION"));
		NHoverTipSet.CreateAndShow(this, tip, HoverTipAlignment.Left);
	}
	protected override void OnPreview()
	{
		ClearHoverTip();
		CreateHoverTip();
	}
	public override void _ExitTree()
	{
		_entry.EntryUpdated -= UpdateVisual;
		_entry.PurchaseFailed -= PurchaseFailed;
		_entry.PurchaseCompleted -= PurchaseCompleted;
		ClearHoverTip();
		base._ExitTree();
	}
}
