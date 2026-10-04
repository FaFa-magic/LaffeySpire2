using Godot;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace LaffeySpire2.LaffeySpire2Code.Characters;

public sealed class LaffeyCardPool : TypeListCardPoolModel, IModColorfulPhilosophersCardPool
{
	public override string Title => LaffeyCharacter.CharacterId;
	public override string EnergyColorName => LaffeyCharacter.CharacterId;

	public override string BigEnergyIconPath =>
		"res://LaffeySpire2/images/packed/sprite_fonts/Laffey_energy_icon_original.png";
	public override string TextEnergyIconPath =>
		"res://LaffeySpire2/images/packed/sprite_fonts/Laffey_energy_icon.png";

	public override Color DeckEntryCardColor => new("806018");
	public override Color EnergyOutlineColor => new("806018");

	private static readonly Material? _poolFrameMaterial = MaterialUtils.CreateUnmodulatedHsvShaderMaterial();
	public override Material? PoolFrameMaterial => _poolFrameMaterial;

	public override bool IsColorless => false;
}

