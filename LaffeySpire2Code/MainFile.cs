using System.Reflection;
using LaffeySpire2.LaffeySpire2Code.Configs;
using LaffeySpire2.LaffeySpire2Code.Patches;
using LaffeySpire2.LaffeySpire2Code.Patchwork;
using LaffeySpire2.LaffeySpire2Code.Relics;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Core;

namespace LaffeySpire2.LaffeySpire2Code;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
	public const string ModId = "LaffeySpire2";

	public static Logger Logger { get; private set; } = null!;

	public static void Initialize()
	{
		var assembly = Assembly.GetExecutingAssembly();

		Logger = RitsuLibFramework.CreateLogger(ModId);
		ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
		RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
		LaffeyConfigPage.Register();
		RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping<LaffeyPillow, HuggyPillowOfBravery>();
		RitsuLibFramework.RegisterArchaicToothTranscendenceMapping<
			Cards.Basic.Strike,
			MegaCrit.Sts2.Core.Models.Cards.Break>(ModId);
		PatchworkBoard.Register();
		PatchworkPieceReward.Register();
		PatchworkActions.Register();

		ModPatcher patcher = RitsuLibFramework.CreatePatcher(ModId, "laffey_patches");
		patcher.RegisterPatch<LaffeyDreamTransitionPatch>();
		patcher.RegisterPatch<LaffeySkinEnumerationPatch>();
		patcher.RegisterPatch<LaffeySkinCardLibrarySelectionPatch>();
		patcher.RegisterPatch<LaffeySharedProgressionLookupPatch>();
		patcher.RegisterPatch<LaffeySkinAncientDialogueLookupPatch>();
		patcher.RegisterPatch<LaffeySharedGameOverProgressionPatch>();
		patcher.RegisterPatch<LaffeySkinSelectPatch>();
		patcher.RegisterPatch<LaffeySkinSelectEmbarkPatch>();
		patcher.RegisterPatch<LaffeySkinSelectUnreadyPatch>();
		patcher.RegisterPatch<LaffeyCombatSpineIdleBootstrapPatch>();
		patcher.RegisterPatch<LaffeyQualityUpgradePatch>();
		patcher.RegisterPatch<LaffeyQualityInspectorContextPatch>();
		patcher.RegisterPatch<LaffeyQualityLibraryPatch>();
		patcher.RegisterPatch<LaffeyQualityInspectorPatch>();
		patcher.RegisterPatch<SolomonSeaRunPatch>();
		patcher.RegisterPatch<SolomonSeaActLoadPatch>();
		patcher.RegisterPatch<SolomonSeaActSerializePatch>();
		patcher.RegisterPatch<SolomonSeaMapPatch>();
		patcher.RegisterPatch<SolomonSeaMapRestorePatch>();
		patcher.RegisterPatch<SolomonSeaFloorCountPatch>();
		patcher.RegisterPatch<SolomonSeaTreasureRoomPatch>();
		patcher.RegisterPatch<SolomonSeaRestSiteActIndexPatch>();
		patcher.RegisterPatch<SolomonSeaRestSiteReadyPatch>();
		patcher.RegisterPatch<PatchworkCombatStartPatch>();
		patcher.RegisterPatch<PatchworkRewardPatch>();
		patcher.RegisterPatch<PatchworkDrawPatch>();
		patcher.RegisterPatch<PatchworkEnergyPatch>();
		patcher.RegisterPatch<PatchworkShopPatch>();
		patcher.RegisterPatch<PatchworkShopEntriesPatch>();
		patcher.RegisterPatch<PatchworkShopSlotsPatch>();
		patcher.RegisterPatch<PatchworkShopNavigationPatch>();
		patcher.RegisterPatch<PatchworkAncientFallbackPatch>();

		if (!patcher.PatchAll())
			throw new InvalidOperationException("Critical patches failed.");
	}
}
