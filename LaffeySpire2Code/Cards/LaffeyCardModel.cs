using LaffeySpire2.LaffeySpire2Code.Characters;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.Cards;

[RegisterCard(typeof(LaffeyCardPool), Inherit = true)]
public abstract class LaffeyCardModel : ModCardTemplate
{
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
			CardType.Attack => "res://JanusSpire2/images/card_frames/janus_attack_1.png",
			CardType.Skill => "res://JanusSpire2/images/card_frames/janus_skill_1.png",
			CardType.Power => "res://JanusSpire2/images/card_frames/janus_power_1.png",
			_ => ""
		}
	);
}
