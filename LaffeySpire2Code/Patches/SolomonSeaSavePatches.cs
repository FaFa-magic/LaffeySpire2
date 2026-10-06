using HarmonyLib;
using LaffeySpire2.LaffeySpire2Code.Acts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;

namespace LaffeySpire2.LaffeySpire2Code.Patches;

public sealed class SolomonSeaActLoadPatch : IPatchMethod
{
	public static string PatchId => "laffey_solomon_sea_act_load";
	public static string Description => "Restore omitted Solomon Sea room collections before native save loading";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(ActModel), nameof(ActModel.FromSave), [typeof(SerializableActModel)])
	];

	[HarmonyPrefix]
	public static void Prefix(SerializableActModel save) => SolomonSeaSave.NormalizeRoomLists(save);
}

public sealed class SolomonSeaActSerializePatch : IPatchMethod
{
	public static string PatchId => "laffey_solomon_sea_act_serialize";
	public static string Description => "Normalize omitted Solomon Sea room collections before native network serialization";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(SerializableActModel), nameof(SerializableActModel.Serialize), [typeof(PacketWriter)])
	];

	[HarmonyPrefix]
	public static void Prefix(SerializableActModel __instance) => SolomonSeaSave.NormalizeRoomLists(__instance);
}
