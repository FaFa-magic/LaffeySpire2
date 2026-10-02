using LaffeySpire2.LaffeySpire2Code.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.Relics;

[RegisterRelic(typeof(LaffeyRelicPool), Inherit = true)]
public abstract class LaffeyRelicModel : ModRelicTemplate
{
	public override RelicAssetProfile AssetProfile => new(
		IconPath: ResolveIconPath("packed"),
		IconOutlinePath: ResolveIconPath("outline"),
		BigIconPath: ResolveIconPath("big")
	);

	private string ResolveIconPath(string size)
	{
		string path = $"res://LaffeySpire2/images/relics/{size}/{GetType().Name}.png";
		string fallbackName = GetType().Name == nameof(HuggyPillowOfBravery)
			? "ShiningCrown"
			: "Coronet";
		return Godot.ResourceLoader.Exists(path)
			? path
			: $"res://JanusSpire2/images/relics/{size}/{fallbackName}.png";
	}
}

