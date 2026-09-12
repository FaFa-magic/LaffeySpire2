namespace LaffeySpire2.LaffeySpire2Code.Characters;

public enum LaffeySkin
{
	Default,
	VariantTwo,
	VariantThree,
	VariantFour,
	VariantFive,
	VariantSix,
	VariantEight,
	VariantNine,
	VariantTen,
	VariantEleven,
	VariantTwelve,
	VariantG,
	VariantH
}

public sealed record LaffeySkinDefinition(
	LaffeySkin Id,
	string SpineSkeletonDataPath,
	string MerchantAnimPath,
	string RestSiteAnimPath);

public static class LaffeySkinManager
{
	private const string AnimationRoot = "res://LaffeySpire2/animations/characters/Laffey";
	private const string SceneRoot = "res://LaffeySpire2/scenes/characters";

	private static readonly IReadOnlyDictionary<LaffeySkin, LaffeySkinDefinition> Skins =
		new Dictionary<LaffeySkin, LaffeySkinDefinition>
		{
			[LaffeySkin.Default] = Create(LaffeySkin.Default, "lafei", ""),
			[LaffeySkin.VariantTwo] = Create(LaffeySkin.VariantTwo, "lafei_2", "_2"),
			[LaffeySkin.VariantThree] = Create(LaffeySkin.VariantThree, "lafei_3", "_3"),
			[LaffeySkin.VariantFour] = Create(LaffeySkin.VariantFour, "lafei_4", "_4"),
			[LaffeySkin.VariantFive] = Create(LaffeySkin.VariantFive, "lafei_5", "_5"),
			[LaffeySkin.VariantSix] = Create(LaffeySkin.VariantSix, "lafei_6", "_6"),
			[LaffeySkin.VariantEight] = Create(LaffeySkin.VariantEight, "lafei_8", "_8"),
			[LaffeySkin.VariantNine] = Create(LaffeySkin.VariantNine, "lafei_9", "_9"),
			[LaffeySkin.VariantTen] = Create(LaffeySkin.VariantTen, "lafei_10", "_10"),
			[LaffeySkin.VariantEleven] = Create(LaffeySkin.VariantEleven, "lafei_11", "_11"),
			[LaffeySkin.VariantTwelve] = Create(LaffeySkin.VariantTwelve, "lafei_12", "_12"),
			[LaffeySkin.VariantG] = Create(LaffeySkin.VariantG, "lafei_g", "_g"),
			[LaffeySkin.VariantH] = Create(LaffeySkin.VariantH, "lafei_h", "_h")
		};

	public static LaffeySkinDefinition GetDefinition(LaffeySkin skin) => Skins[skin];

	private static LaffeySkinDefinition Create(LaffeySkin id, string resourceName, string sceneSuffix)
	{
		return new(
			id,
			$"{AnimationRoot}/{resourceName}_spine.tres",
			$"{SceneRoot}/laffey_merchant{sceneSuffix}.tscn",
			$"{SceneRoot}/laffey_rest_site{sceneSuffix}.tscn");
	}
}
