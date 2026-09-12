using LaffeySpire2.LaffeySpire2Code.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.Relics;

[RegisterCharacterStarterRelic(typeof(LaffeyCharacter))]
public sealed class LaffeyPillow : LaffeyPillowRelic
{
    protected override int CardThreshold => 5;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://JanusSpire2/images/relics/packed/Coronet.png",
        IconOutlinePath: "res://JanusSpire2/images/relics/outline/Coronet.png",
        BigIconPath: "res://JanusSpire2/images/relics/big/Coronet.png"
    );
}
