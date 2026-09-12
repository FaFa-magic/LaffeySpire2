using LaffeySpire2.LaffeySpire2Code.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.Potions;

[RegisterPotion(typeof(LaffeyPotionPool), Inherit = true)]
public abstract class LaffeyPotionModel : ModPotionTemplate
{
	public override PotionAssetProfile AssetProfile => new(
		ImagePath: $"res://JanusSpire2/images/potions/big/{GetType().Name}.png",
		OutlinePath: $"res://JanusSpire2/images/potions/outline/{GetType().Name}.png"
	);
}

