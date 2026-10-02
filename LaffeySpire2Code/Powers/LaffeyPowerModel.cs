using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.Powers;

[RegisterPower(Inherit = true)]
public abstract class LaffeyPowerModel : ModPowerTemplate
{
	public override PowerAssetProfile AssetProfile => new(
		IconPath: ResolveIconPath(),
		BigIconPath: ResolveIconPath()
	);

	private string ResolveIconPath()
	{
		string path = $"res://LaffeySpire2/images/powers/big/{GetType().Name}.png";
		return Godot.ResourceLoader.Exists(path)
			? path
			: "res://JanusSpire2/images/powers/big/AngelPrayerPower.png";
	}
}

