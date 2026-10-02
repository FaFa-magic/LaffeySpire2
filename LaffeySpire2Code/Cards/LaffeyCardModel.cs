using LaffeySpire2.LaffeySpire2Code.Characters;
using LaffeySpire2.LaffeySpire2Code.CardQuality;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.Cards;

[RegisterCard(typeof(LaffeyCardPool), Inherit = true)]
public abstract class LaffeyCardModel : ModCardTemplate
{
	private int _qualityRank;
	private LaffeyQualityConfiguration _appliedQualityConfiguration = LaffeyQualityConfiguration.Empty;

	public CardRarity NativeRarity => base.Rarity;
	public bool HasQualityVersions => NativeRarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare;
	public override CardRarity Rarity => HasQualityVersions && _qualityRank != 0
		? LaffeyQuality.RarityForRank(_qualityRank)
		: NativeRarity;

	[SavedProperty]
	public int QualityRank
	{
		get => _qualityRank;
		set
		{
			AssertMutable();
			if (value is < 0 or > 3 || !HasQualityVersions && value != 0)
				throw new ArgumentOutOfRangeException(nameof(value));
			CardRarity previous = Rarity;
			_qualityRank = value;
			RefreshQualityConfiguration();
			if (previous != Rarity)
				OnQualityChanged(previous, Rarity);
		}
	}

	protected virtual LaffeyQualityConfiguration GetQualityConfiguration(CardRarity quality, bool upgraded)
		=> LaffeyQualityConfiguration.Empty;

	protected virtual void ConfigureQualityMechanics()
	{
	}

	protected virtual void OnQualityChanged(CardRarity previous, CardRarity current)
	{
	}

	protected T QualityValue<T>(T common, T uncommon, T rare) => Rarity switch
	{
		CardRarity.Common => common,
		CardRarity.Uncommon => uncommon,
		CardRarity.Rare => rare,
		_ => throw new InvalidOperationException("This card has no quality versions.")
	};

	public void RefreshQualityConfiguration(bool resetAdjustments = false)
	{
		AssertMutable();
		if (!HasQualityVersions)
			return;
		if (resetAdjustments)
			_appliedQualityConfiguration = LaffeyQualityConfiguration.Empty;
		LaffeyQualityConfiguration next = GetQualityConfiguration(Rarity, IsUpgraded);
		int costDelta = next.EnergyCostAdjustment - _appliedQualityConfiguration.EnergyCostAdjustment;
		if (costDelta != 0)
			EnergyCost.SetCustomBaseCost(EnergyCost.GetWithModifiers(CostModifiers.None) + costDelta);
		bool valuesChanged = false;
		foreach (string name in next.Values.Keys.Union(_appliedQualityConfiguration.Values.Keys))
		{
			decimal delta = next.ValueAdjustment(name) - _appliedQualityConfiguration.ValueAdjustment(name);
			if (delta == 0)
				continue;
			DynamicVars[name].BaseValue += delta;
			valuesChanged = true;
		}
		_appliedQualityConfiguration = next;
		if (valuesChanged)
			DynamicVars.RecalculateForUpgradeOrEnchant();
		ConfigureQualityMechanics();
	}

	protected LaffeyCardModel(int energyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary = true)
		: base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
	{
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"res://JanusSpire2/images/cards/{GetType().Name}.png",
		BannerTexturePath: "res://JanusSpire2/images/card_frames/janus_Banner.png",
		AncientBannerPath: "res://JanusSpire2/images/card_frames/janus_Banner.png",
		AncientBorderPath: "res://JanusSpire2/images/card_frames/janus_ancient.png",
		AncientBorderMaterialPath: "res://JanusSpire2/materials/cards/janus_ancient_border_opaque.tres",
		FramePath: Type switch
		{
			CardType.Attack => "res://LaffeySpire2/images/card_frames/laffey_attack.png",
			CardType.Skill => "res://LaffeySpire2/images/card_frames/laffey_skill.png",
			CardType.Power => "res://LaffeySpire2/images/card_frames/laffey_power.png",
			_ => ""
		}
	);
}
