using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.Relics;

public sealed class HuggyPillowOfBravery : LaffeyPillowRelic
{
    protected override int CardThreshold => 4;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://JanusSpire2/images/relics/packed/ShiningCrown.png",
        IconOutlinePath: "res://JanusSpire2/images/relics/outline/ShiningCrown.png",
        BigIconPath: "res://JanusSpire2/images/relics/big/ShiningCrown.png"
    );
}
