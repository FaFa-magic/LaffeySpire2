using System.Reflection.Emit;
using HarmonyLib;
using LaffeySpire2.LaffeySpire2Code.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib;
using STS2RitsuLib.Patching.Models;

namespace LaffeySpire2.LaffeySpire2Code.Patches;

/// <summary>
/// Skin variants are persistence identities, not additional gameplay characters.
/// </summary>
public sealed class LaffeySkinEnumerationPatch : IPatchMethod
{
	public static string PatchId => "laffey_skin_character_enumeration";
	public static string Description => "Exclude auxiliary Laffey skin models from global character lists";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(ModelDb), nameof(ModelDb.AllCharacters), null, MethodType.Getter)
	];

	[HarmonyAfter(Const.FrameworkContentRegistryHarmonyId)]
	[HarmonyPriority(Priority.Last)]
	[HarmonyPostfix]
	public static void Postfix(ref IEnumerable<CharacterModel> __result)
	{
		__result = __result.Where(character =>
			character is not LaffeyCharacter laffey || laffey.CurrentSkin == LaffeySkin.Default);
	}
}

public static class LaffeySharedProgression
{
	public static ModelId GetProgressionId(ModelId characterId)
	{
		return IsSkinVariantId(characterId) ? ModelDb.GetId<LaffeyCharacter>() : characterId;
	}

	private static bool IsSkinVariantId(ModelId characterId)
	{
		return characterId == ModelDb.GetId<LaffeyVariantTwo>()
		       || characterId == ModelDb.GetId<LaffeyVariantThree>()
		       || characterId == ModelDb.GetId<LaffeyVariantFour>()
		       || characterId == ModelDb.GetId<LaffeyVariantFive>()
		       || characterId == ModelDb.GetId<LaffeyVariantSix>()
		       || characterId == ModelDb.GetId<LaffeyVariantEight>()
		       || characterId == ModelDb.GetId<LaffeyVariantNine>()
		       || characterId == ModelDb.GetId<LaffeyVariantTen>()
		       || characterId == ModelDb.GetId<LaffeyVariantEleven>()
		       || characterId == ModelDb.GetId<LaffeyVariantTwelve>()
		       || characterId == ModelDb.GetId<LaffeyVariantG>()
		       || characterId == ModelDb.GetId<LaffeyVariantH>();
	}
}

public sealed class LaffeySharedProgressionLookupPatch : IPatchMethod
{
	public static string PatchId => "laffey_skin_shared_progression";
	public static string Description => "Share Laffey progression across Spine skins";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(ProgressState), nameof(ProgressState.GetOrCreateCharacterStats), [typeof(ModelId)]),
		new(typeof(ProgressState), nameof(ProgressState.GetStatsForCharacter), [typeof(ModelId)])
	];

	[HarmonyPrefix]
	public static void Prefix(ref ModelId characterId)
	{
		characterId = LaffeySharedProgression.GetProgressionId(characterId);
	}
}

public sealed class LaffeySharedGameOverProgressionPatch : IPatchMethod
{
	public static string PatchId => "laffey_skin_shared_game_over_progression";
	public static string Description => "Store Laffey skin badges in shared character progression";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NGameOverScreen), "SaveBadgesToProgress", null)
	];

	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var characterIdGetter = AccessTools.PropertyGetter(typeof(AbstractModel), nameof(AbstractModel.Id));
		var progressionMapper = AccessTools.DeclaredMethod(
			typeof(LaffeySharedProgression),
			nameof(LaffeySharedProgression.GetProgressionId));
		var mapped = false;

		foreach (var instruction in instructions)
		{
			yield return instruction;

			if (instruction.Calls(characterIdGetter))
			{
				mapped = true;
				yield return new CodeInstruction(OpCodes.Call, progressionMapper);
			}
		}

		if (!mapped)
			MainFile.Logger.Error("Unable to patch the game-over Laffey skin progression lookup.");
	}
}
