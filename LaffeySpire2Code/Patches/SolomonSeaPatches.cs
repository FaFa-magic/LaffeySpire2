using System.Reflection;
using HarmonyLib;
using LaffeySpire2.LaffeySpire2Code.Acts;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace LaffeySpire2.LaffeySpire2Code.Patches;

public sealed class SolomonSeaRunPatch : IPatchMethod
{
	public static string PatchId => "laffey_solomon_sea_run";
	public static string Description => "Append Solomon Sea after vanilla room generation or loading";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(RunManager), nameof(RunManager.GenerateRooms)),
		new(typeof(RunManager), "InitializeSavedRun", [typeof(SerializableRun)])
	];

	[HarmonyPostfix]
	public static void Postfix(RunManager __instance) => SolomonSeaRun.EnsureAct(__instance.DebugOnlyGetState());
}

public sealed class SolomonSeaMapPatch : IPatchMethod
{
	public static string PatchId => "laffey_solomon_sea_map";
	public static string Description => "Create the fixed treasure, shop, rest, elite, elite, rest and boss route for Solomon Sea";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(ActModel), nameof(ActModel.CreateMap), [typeof(RunState), typeof(bool)])
	];

	[HarmonyPrefix]
	public static bool Prefix(ActModel __instance, ref ActMap __result)
	{
		if (__instance is not SolomonSea)
			return true;

		__result = new SolomonSeaMap();
		return false;
	}
}

public sealed class SolomonSeaFloorCountPatch : IPatchMethod
{
	public static string PatchId => "laffey_solomon_sea_floor_count";
	public static string Description => "Keep Solomon Sea at seven floors in singleplayer and multiplayer";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(ActModel), nameof(ActModel.GetNumberOfRooms), [typeof(bool)]),
		new(typeof(ActModel), nameof(ActModel.GetNumberOfFloors), [typeof(bool)])
	];

	[HarmonyPrefix]
	public static bool Prefix(ActModel __instance, MethodBase __originalMethod, ref int __result)
	{
		if (__instance is not SolomonSea)
			return true;

		__result = __originalMethod.Name == nameof(ActModel.GetNumberOfFloors)
			? SolomonSeaMap.RoomCount
			: SolomonSeaMap.RoomCount - 1;
		return false;
	}
}

public sealed class SolomonSeaMapRestorePatch : IPatchMethod
{
	public static string PatchId => "laffey_solomon_sea_map_restore";
	public static string Description => "Restore fixed point flags before applying late map hooks to a saved Solomon Sea map";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(Hook), nameof(Hook.ModifyGeneratedMapLate), [typeof(IRunState), typeof(ActMap), typeof(int)])
	];

	[HarmonyPrefix]
	public static void Prefix(IRunState runState, ActMap map, int actIndex)
	{
		if (actIndex < 0 || actIndex >= runState.Acts.Count || runState.Acts[actIndex] is not SolomonSea)
			return;

		map.StartingMapPoint.CanBeModified = false;
		map.BossMapPoint.CanBeModified = false;
		foreach (MapPoint point in map.GetAllMapPoints())
			point.CanBeModified = false;
	}
}

public sealed class SolomonSeaTreasureRoomPatch : IPatchMethod
{
	public static string PatchId => "laffey_solomon_sea_treasure_room";
	public static string Description => "Use the vanilla treasure room without its three-act constructor restriction";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(RunManager), "CreateRoom", [typeof(RoomType), typeof(MapPointType), typeof(AbstractModel)])
	];

	[HarmonyPrefix]
	public static bool Prefix(RunManager __instance, RoomType roomType, ref AbstractRoom __result)
	{
		if (__instance.DebugOnlyGetState()?.Act is not SolomonSea || roomType != RoomType.Treasure)
			return true;

		__result = new TreasureRoom(2);
		return false;
	}
}

public sealed class SolomonSeaRestSiteActIndexPatch : IPatchMethod
{
	public static string PatchId => "laffey_solomon_sea_rest_site_act_index";
	public static string Description => "Use the Glory animation index only inside fourth-act rest character initialization";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(RunState), nameof(RunState.CurrentActIndex), null, MethodType.Getter)
	];

	[HarmonyPostfix]
	public static void Postfix(RunState __instance, ref int __result)
	{
		if (__result == 3 && SolomonSeaRestSiteContext.IsActive(__instance))
			__result = 2;
	}
}

public sealed class SolomonSeaRestSiteReadyPatch : IPatchMethod
{
	public static string PatchId => "laffey_solomon_sea_rest_site_ready";
	public static string Description => "Keep native rest characters compatible with Solomon Sea in mixed multiplayer parties";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))
	];

	[HarmonyPrefix]
	[HarmonyPriority(Priority.First)]
	public static void Prefix(NRestSiteCharacter __instance, out bool __state) =>
		__state = SolomonSeaRestSiteContext.Enter(__instance.Player.RunState);

	[HarmonyFinalizer]
	public static void Finalizer(bool __state) => SolomonSeaRestSiteContext.Exit(__state);
}
