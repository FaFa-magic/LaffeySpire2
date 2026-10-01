using HarmonyLib;
using LaffeySpire2.LaffeySpire2Code.Characters;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Runs;
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

public sealed class LaffeySkinCardLibrarySelectionPatch : IPatchMethod
{
	public static string PatchId => "laffey_skin_card_library_selection";
	public static string Description => "Use the Laffey card-pool filter for the active skin variant";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NCardLibrary), nameof(NCardLibrary.OnSubmenuOpened))
	];

	[HarmonyPrefix]
	public static void Prefix(
		IRunState? ____runState,
		Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters,
		NCardPoolFilter ____ironcladFilter)
	{
		CharacterModel? character = LocalContext.GetMe(____runState)?.Character;
		if (character is not LaffeySkinVariant || ____cardPoolFilters.ContainsKey(character))
			return;

		ModelId baseId = ModelDb.GetId<LaffeyCharacter>();
		NCardPoolFilter? filter = ____cardPoolFilters
			.FirstOrDefault(entry => entry.Key.Id == baseId).Value;
		if (filter == null)
		{
			MainFile.Logger.Warn("[CardLibrary] Laffey pool filter is unavailable; selecting the vanilla fallback for this skin.");
			filter = ____ironcladFilter;
		}

		____cardPoolFilters[character] = filter;
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

public sealed class LaffeySkinAncientDialogueLookupPatch : IPatchMethod
{
	public static string PatchId => "laffey_skin_ancient_dialogue_lookup";
	public static string Description => "Use base Laffey dialogue for every Laffey skin variant";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(AncientDialogueSet), nameof(AncientDialogueSet.GetValidDialogues),
			[typeof(ModelId), typeof(int), typeof(int), typeof(bool)])
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

	[HarmonyPrefix]
	[HarmonyPriority(Priority.First)]
	public static void Prefix(Player ____localPlayer, out IDisposable? __state)
	{
		__state = null;
		ModelId skinId = ____localPlayer.Character.Id;
		ModelId progressionId = LaffeySharedProgression.GetProgressionId(skinId);
		if (skinId == progressionId)
		{
			return;
		}

		ProgressState progress = SaveManager.Instance.Progress;
		if (progress.CharacterStats is not IDictionary<ModelId, CharacterStats> characterStats)
		{
			throw new InvalidOperationException(
				"The game-over progression dictionary is not mutable; Laffey cannot install its scoped skin alias.");
		}

		CharacterStats sharedStats = progress.GetOrCreateCharacterStats(progressionId);
		bool hadPrevious = characterStats.TryGetValue(skinId, out CharacterStats? previous);
		characterStats[skinId] = sharedStats;
		__state = new CharacterStatsAliasLease(characterStats, skinId, hadPrevious, previous);
	}

	public static void Finalizer(IDisposable? __state)
	{
		__state?.Dispose();
	}

	private sealed class CharacterStatsAliasLease(
		IDictionary<ModelId, CharacterStats> characterStats,
		ModelId skinId,
		bool hadPrevious,
		CharacterStats? previous) : IDisposable
	{
		public void Dispose()
		{
			if (hadPrevious)
			{
				characterStats[skinId] = previous!;
			}
			else
			{
				characterStats.Remove(skinId);
			}
		}
	}
}
