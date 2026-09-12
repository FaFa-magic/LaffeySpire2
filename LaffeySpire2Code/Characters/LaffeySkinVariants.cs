using STS2RitsuLib.Interop.AutoRegistration;

namespace LaffeySpire2.LaffeySpire2Code.Characters;

public abstract class LaffeySkinVariant : LaffeyCharacter
{
	public sealed override bool HideFromVanillaCharacterSelect => true;
	public sealed override bool AllowInVanillaRandomCharacterSelect => false;
	public sealed override bool HideInCardLibraryCompendium => true;
}

[RegisterCharacter]
public sealed class LaffeyVariantTwo : LaffeySkinVariant
{
	public override LaffeySkin CurrentSkin => LaffeySkin.VariantTwo;
}

[RegisterCharacter]
public sealed class LaffeyVariantThree : LaffeySkinVariant
{
	public override LaffeySkin CurrentSkin => LaffeySkin.VariantThree;
}

[RegisterCharacter]
public sealed class LaffeyVariantFour : LaffeySkinVariant
{
	public override LaffeySkin CurrentSkin => LaffeySkin.VariantFour;
}

[RegisterCharacter]
public sealed class LaffeyVariantFive : LaffeySkinVariant
{
	public override LaffeySkin CurrentSkin => LaffeySkin.VariantFive;
}

[RegisterCharacter]
public sealed class LaffeyVariantSix : LaffeySkinVariant
{
	public override LaffeySkin CurrentSkin => LaffeySkin.VariantSix;
}

[RegisterCharacter]
public sealed class LaffeyVariantEight : LaffeySkinVariant
{
	public override LaffeySkin CurrentSkin => LaffeySkin.VariantEight;
}

[RegisterCharacter]
public sealed class LaffeyVariantNine : LaffeySkinVariant
{
	public override LaffeySkin CurrentSkin => LaffeySkin.VariantNine;
}

[RegisterCharacter]
public sealed class LaffeyVariantTen : LaffeySkinVariant
{
	public override LaffeySkin CurrentSkin => LaffeySkin.VariantTen;
}

[RegisterCharacter]
public sealed class LaffeyVariantEleven : LaffeySkinVariant
{
	public override LaffeySkin CurrentSkin => LaffeySkin.VariantEleven;
}

[RegisterCharacter]
public sealed class LaffeyVariantTwelve : LaffeySkinVariant
{
	public override LaffeySkin CurrentSkin => LaffeySkin.VariantTwelve;
}

[RegisterCharacter]
public sealed class LaffeyVariantG : LaffeySkinVariant
{
	public override LaffeySkin CurrentSkin => LaffeySkin.VariantG;
}

[RegisterCharacter]
public sealed class LaffeyVariantH : LaffeySkinVariant
{
	public override LaffeySkin CurrentSkin => LaffeySkin.VariantH;
}
