using LaffeySpire2.LaffeySpire2Code.Characters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace LaffeySpire2.LaffeySpire2Code.Relics;

[RegisterCharacterStarterRelic(typeof(LaffeyCharacter))]
public sealed class LaffeyPillow : LaffeyPillowRelic
{
    protected override int CardThreshold => 5;
}
