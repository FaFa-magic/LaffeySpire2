using LaffeySpire2.LaffeySpire2Code.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.Relics;

[RegisterRelic(typeof(LaffeyRelicPool), Inherit = true)]
public abstract class LaffeyRelicModel : ModRelicTemplate
{
	public override RelicAssetProfile AssetProfile => new(
		IconPath: $"res://JanusSpire2/images/relics/packed/{GetType().Name}.png",
		IconOutlinePath: $"res://JanusSpire2/images/relics/outline/{GetType().Name}.png",
		BigIconPath: $"res://JanusSpire2/images/relics/big/{GetType().Name}.png"
	);
}

