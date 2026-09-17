using System.Reflection;
using LaffeySpire2.LaffeySpire2Code.Patches;
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
		RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping<LaffeyPillow, HuggyPillowOfBravery>();

		ModPatcher patcher = RitsuLibFramework.CreatePatcher(ModId, "laffey_patches");
		patcher.RegisterPatch<LaffeySkinEnumerationPatch>();
		patcher.RegisterPatch<LaffeySharedProgressionLookupPatch>();
		patcher.RegisterPatch<LaffeySkinAncientDialogueLookupPatch>();
		patcher.RegisterPatch<LaffeySharedGameOverProgressionPatch>();
		patcher.RegisterPatch<LaffeySkinSelectPatch>();
		patcher.RegisterPatch<LaffeySkinSelectEmbarkPatch>();
		patcher.RegisterPatch<LaffeySkinSelectUnreadyPatch>();
		patcher.RegisterPatch<LaffeyCombatSpineIdleBootstrapPatch>();

		if (!patcher.PatchAll())
			throw new InvalidOperationException("Critical patches failed.");
	}
}
