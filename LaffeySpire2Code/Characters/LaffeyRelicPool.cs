using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.Characters;

public sealed class LaffeyRelicPool : TypeListRelicPoolModel
{
	public override string EnergyColorName => LaffeyCharacter.CharacterId;

	public override string BigEnergyIconPath =>
		"res://JanusSpire2/images/packed/sprite_fonts/Janus_energy_icon_original.png";
	public override string TextEnergyIconPath =>
		"res://JanusSpire2/images/packed/sprite_fonts/Janus_energy_icon.png";
}

