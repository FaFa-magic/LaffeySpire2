param(
    [string]$GameDataDir = 'D:/STEAM/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64',
    [string]$RitsuPath = 'C:/Users/15832/.nuget/packages/sts2.ritsulib/0.5.20/lib/net9.0/STS2-RitsuLib.dll'
)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$sdkDir = Join-Path 'C:/Program Files/dotnet/sdk' ((& dotnet --version).Trim())
Add-Type -Path (Join-Path $sdkDir 'Roslyn/bincore/Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $sdkDir 'Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll')
$parseOptions = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithLanguageVersion([Microsoft.CodeAnalysis.CSharp.LanguageVersion]::Preview)
$trees = [System.Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]]::new()
$trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText('global using System; global using System.Collections.Generic; global using System.IO; global using System.Linq; global using System.Threading.Tasks;', $parseOptions))
foreach ($file in Get-ChildItem (Join-Path $projectRoot 'LaffeySpire2Code') -Recurse -Filter '*.cs') {
    $trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($file.FullName), $parseOptions, $file.FullName))
}
$checks = @'
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using HarmonyLib;
using LaffeySpire2.LaffeySpire2Code.Acts;
using LaffeySpire2.LaffeySpire2Code.Characters;
using LaffeySpire2.LaffeySpire2Code.Patches;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Content;

public static class SolomonSeaChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Set(object obj, string field, object value) =>
        AccessTools.Field(obj.GetType(), field).SetValue(obj, value);

    private static RunState State(params Type[] characters)
    {
        var state = (RunState)RuntimeHelpers.GetUninitializedObject(typeof(RunState));
        var players = characters.Select(type =>
        {
            var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
            Set(player, "<Character>k__BackingField", RuntimeHelpers.GetUninitializedObject(type));
            return player;
        }).ToList();
        Set(state, "_players", players);
        Set(state, "_currentRooms", new List<AbstractRoom>());
        Set(state, "_visitedMapCoords", new List<MapCoord>());
        AccessTools.PropertySetter(typeof(RunState), nameof(RunState.Acts)).Invoke(state,
            new object[] { new ActModel[] { ModelDb.Act<Overgrowth>().ToMutable(), ModelDb.Act<Hive>().ToMutable(), ModelDb.Act<Glory>().ToMutable() } });
        return state;
    }

    private static byte[] Packet(ActMap map)
    {
        var writer = new PacketWriter();
        SerializableActMap.FromActMap(map).Serialize(writer);
        return writer.Buffer.Take((writer.BitPosition + 7) / 8).ToArray();
    }

    private static void SeedWireIds()
    {
        ModelIdSerializationCache.ResetForTest();
        foreach (string kind in new[] { "Category", "Entry" })
        {
            string stem = char.ToLowerInvariant(kind[0]) + kind.Substring(1);
            var names = (List<string>)AccessTools.Field(typeof(ModelIdSerializationCache), "_netIdTo" + kind + "NameMap").GetValue(null)!;
            var ids = (Dictionary<string, int>)AccessTools.Field(typeof(ModelIdSerializationCache), "_" + stem + "NameToNetIdMap").GetValue(null)!;
            foreach (var model in ModelDb.All.OrderBy(model => model.Id.ToString(), StringComparer.Ordinal))
            {
                string name = kind == "Category" ? model.Id.Category : model.Id.Entry;
                if (!ids.ContainsKey(name)) { ids[name] = names.Count; names.Add(name); }
            }
            AccessTools.PropertySetter(typeof(ModelIdSerializationCache), kind + "IdBitSize").Invoke(null,
                new object[] { (int)Math.Ceiling(Math.Log2(names.Count)) });
        }
        AccessTools.Field(typeof(ModelIdSerializationCache), "_initialized").SetValue(null, true);
    }

    private static byte[] ActPacket(SerializableActModel saved)
    {
        SolomonSeaActSerializePatch.Prefix(saved);
        var writer = new PacketWriter();
        saved.Serialize(writer);
        return writer.Buffer.Take((writer.BitPosition + 7) / 8).ToArray();
    }

    private static void DiskAndNetwork(RunState state, ActModel act, ActMap map)
    {
        var saved = act.ToSave();
        saved.SavedMap = SerializableActMap.FromActMap(map);
        saved.SerializableRooms.BossEncountersVisited = 1;
        string json = JsonSerializer.Serialize(saved, JsonSerializationUtility.Options);
        for (int mask = 0; mask < 8; mask++)
        {
            var node = JsonNode.Parse(json)!;
            string[] keys = { "event_ids", "normal_encounter_ids", "elite_encounter_ids" };
            for (int i = 0; i < keys.Length; i++)
            {
                node["rooms"]!.AsObject().Remove(keys[i]);
                if ((mask & (1 << i)) != 0) node["rooms"]![keys[i]] = new JsonArray();
            }
            var disk = JsonSerializer.Deserialize<SerializableActModel>(node.ToJsonString(), JsonSerializationUtility.Options)!;
            SolomonSeaActLoadPatch.Prefix(disk);
            var diskAct = ActModel.FromSave(disk);
            Check(diskAct.ToSave().SerializableRooms.BossEncountersVisited == 1 && diskAct.BossEncounter == act.BossEncounter,
                "Missing-list compatibility changed boss progress.");
            Check(disk.SerializableRooms.EventIds.Count == 0 && disk.SerializableRooms.NormalEncounterIds.Count == 0
                && disk.SerializableRooms.EliteEncounterIds.SequenceEqual(SolomonSea.InitialEliteEncounters.Select(encounter => encounter.Id)),
                "Missing lists or elite encounters were not restored.");
            var peerDisk = JsonSerializer.Deserialize<SerializableActModel>(node.ToJsonString(), JsonSerializationUtility.Options)!;
            Check(ActPacket(peerDisk).SequenceEqual(ActPacket(WithSavedMap(diskAct, disk.SavedMap))), "Peer room state differs.");
            var reader = new PacketReader();
            reader.Reset(ActPacket(peerDisk));
            var transmitted = new SerializableActModel();
            transmitted.Deserialize(reader);
            var peerAct = ActModel.FromSave(transmitted);
            Check(peerAct.ToSave().SerializableRooms.BossEncountersVisited == 1 && peerAct.BossEncounter == act.BossEncounter,
                "Network load lost boss progress.");
            var peerMap = new SavedActMap(transmitted.SavedMap!);
            SolomonSeaMapRestorePatch.Prefix(state, peerMap, 3);
            Check(Packet(peerMap).SequenceEqual(Packet(map)), "Network load lost the saved map.");
        }
        LegacySaves(state, act);
        var populated = act.ToSave();
        var events = populated.SerializableRooms.EventIds = new() { ModelDb.GetId<TheArchitect>() };
        var normal = populated.SerializableRooms.NormalEncounterIds = new() { ModelDb.GetId<DevotedSculptorWeak>() };
        var elites = populated.SerializableRooms.EliteEncounterIds = new() { ModelDb.GetId<KnightsElite>() };
        populated.SerializableRooms.EventsVisited = 2;
        populated.SerializableRooms.NormalEncountersVisited = 3;
        populated.SerializableRooms.EliteEncountersVisited = 4;
        populated.SerializableRooms.SecondBossId = ModelDb.GetId<AeonglassBoss>();
        SolomonSeaActLoadPatch.Prefix(populated);
        Check(ReferenceEquals(events, populated.SerializableRooms.EventIds) && ReferenceEquals(normal, populated.SerializableRooms.NormalEncounterIds)
            && ReferenceEquals(elites, populated.SerializableRooms.EliteEncounterIds), "Existing lists were overwritten.");
        var populatedRestore = ActModel.FromSave(populated).ToSave().SerializableRooms;
        Check(populatedRestore.EventsVisited == 2 && populatedRestore.NormalEncountersVisited == 3 && populatedRestore.EliteEncountersVisited == 4
            && populatedRestore.SecondBossId == ModelDb.GetId<AeonglassBoss>(), "Existing counters or second boss changed.");
        var vanilla = new SerializableActModel { Id = ModelDb.GetId<Glory>(), SerializableRooms = new SerializableRoomSet() };
        SolomonSeaActLoadPatch.Prefix(vanilla);
        SolomonSeaActSerializePatch.Prefix(vanilla);
        Check(vanilla.SerializableRooms.EventIds == null && vanilla.SerializableRooms.NormalEncounterIds == null
            && vanilla.SerializableRooms.EliteEncounterIds == null, "Unrelated act save was modified.");
        Console.WriteLine("PASS: native act JSON omissions reproduced; all eight missing/empty list combinations restored; native act network round-trip and peer bytes match; existing lists, counters, bosses and map preserved; unrelated saves unchanged.");
    }

    private static void LegacySaves(RunState state, ActModel act)
    {
        foreach (bool generated in new[] { false, true })
        {
            var saved = act.ToSave();
            saved.SerializableRooms.EliteEncounterIds.Clear();
            saved.SerializableRooms.BossEncountersVisited = 1;
            if (generated)
            {
                MapPoint[] points =
                {
                    new(3, 0) { PointType = MapPointType.Treasure, CanBeModified = false },
                    new(3, 1) { PointType = MapPointType.Shop, CanBeModified = false },
                    new(3, 2) { PointType = MapPointType.RestSite, CanBeModified = false },
                    new(3, 3) { PointType = MapPointType.Boss, CanBeModified = false }
                };
                for (int i = 1; i < points.Length; i++) points[i - 1].AddChildPoint(points[i]);
                saved.SavedMap = new SerializableActMap
                {
                    GridWidth = 7, GridHeight = 3,
                    StartingPoint = SerializableMapPoint.FromMapPoint(points[0]),
                    BossPoint = SerializableMapPoint.FromMapPoint(points[3]),
                    Points = points.Skip(1).Take(2).Select(SerializableMapPoint.FromMapPoint).ToList(),
                    StartMapPointCoords = new() { points[1].coord }
                };
            }
            var disk = JsonSerializer.Deserialize<SerializableActModel>(
                JsonSerializer.Serialize(saved, JsonSerializationUtility.Options), JsonSerializationUtility.Options)!;
            var originalMap = disk.SavedMap;
            SolomonSeaActLoadPatch.Prefix(disk);
            SolomonSeaActSerializePatch.Prefix(disk);
            Check(ReferenceEquals(originalMap, disk.SavedMap), "Legacy map was replaced.");
            var restored = ActModel.FromSave(disk);
            Check(restored.ToSave().SerializableRooms.EliteEncounterIds.Count == (generated ? 0 : 2)
                && restored.ToSave().SerializableRooms.BossEncountersVisited == 1, "Legacy progress or encounter migration is wrong.");
            if (generated)
            {
                var restoredMap = new SavedActMap(disk.SavedMap!);
                SolomonSeaMapRestorePatch.Prefix(state, restoredMap, 3);
                Check(restoredMap.GetRowCount() == 3 && restoredMap.GetAllMapPoints().Count() == 2
                    && restoredMap.GetAllMapPoints().All(point => point.PointType != MapPointType.Elite)
                    && restoredMap.BossMapPoint.coord == new MapCoord(3, 3), "Legacy route or coordinates changed.");
                Check(Packet(restoredMap).SequenceEqual(Packet(new SavedActMap(saved.SavedMap!))), "Legacy topology changed.");
            }
            else
                Check(restored.PullNextEncounter(RoomType.Elite) == SolomonSea.InitialEliteEncounters.First(), "Legacy ungenerated act has no elite encounter.");
            var reader = new PacketReader();
            reader.Reset(ActPacket(disk));
            var transmitted = new SerializableActModel();
            transmitted.Deserialize(reader);
            Check(transmitted.SerializableRooms.EliteEncounterIds.SequenceEqual(disk.SerializableRooms.EliteEncounterIds), "Legacy peer encounter state differs.");
        }
        Console.WriteLine("PASS: legacy generated four-room maps preserved; ungenerated legacy acts receive the new elite list; native peer migration matches.");
    }

    private static void EliteProgress(ActModel act)
    {
        var expected = SolomonSea.InitialEliteEncounters.ToArray();
        Check(act.AllEliteEncounters.SequenceEqual(expected) && expected.All(encounter => encounter.RoomType == RoomType.Elite), "Invalid elite encounter pool.");
        var run = ActModel.FromSave(act.ToSave());
        Check(run.PullNextEncounter(RoomType.Elite) == expected[0], "Wrong first elite encounter.");
        run.MarkRoomVisited(RoomType.Elite);
        var disk = JsonSerializer.Deserialize<SerializableActModel>(
            JsonSerializer.Serialize(run.ToSave(), JsonSerializationUtility.Options), JsonSerializationUtility.Options)!;
        SolomonSeaActLoadPatch.Prefix(disk);
        var restored = ActModel.FromSave(disk);
        Check(restored.PullNextEncounter(RoomType.Elite) == expected[1] && restored.ToSave().SerializableRooms.EliteEncountersVisited == 1,
            "Disk restore replayed the first elite or lost its progress.");
        var reader = new PacketReader();
        reader.Reset(ActPacket(disk));
        var transmitted = new SerializableActModel();
        transmitted.Deserialize(reader);
        var peer = ActModel.FromSave(transmitted);
        Check(peer.PullNextEncounter(RoomType.Elite) == expected[1] && ActPacket(peer.ToSave()).SequenceEqual(ActPacket(restored.ToSave())),
            "Peer drew a different second elite encounter.");
        foreach (var instance in new[] { restored, peer })
        {
            instance.MarkRoomVisited(RoomType.Elite);
            Check(instance.ToSave().SerializableRooms.EliteEncountersVisited == 2 && instance.BossEncounter == act.BossEncounter,
                "Completing both elites changed boss state or lost elite progress.");
        }
        Console.WriteLine("PASS: both native elite encounters resolve in order; disk and native network restore retain elite progress and match the peer.");
    }

    private static SerializableActModel WithSavedMap(ActModel act, SerializableActMap? map)
    {
        var saved = act.ToSave();
        saved.SavedMap = map;
        return saved;
    }

    private static void Route(ActMap map)
    {
        var types = new[] { MapPointType.Treasure, MapPointType.Shop, MapPointType.RestSite, MapPointType.Elite, MapPointType.Elite, MapPointType.RestSite, MapPointType.Boss };
        Check(map.GetColumnCount() == 7 && map.GetRowCount() == types.Length - 1, "Wrong dimensions.");
        Check(map.GetAllMapPoints().Count() == types.Length - 2 && map.SecondBossMapPoint == null, "Unexpected grid point or extra boss.");
        MapPoint point = map.StartingMapPoint;
        for (int i = 0; i < types.Length; i++)
        {
            Check(point.PointType == types[i] && point.coord == new MapCoord(3, i), "Wrong room order.");
            Check(!point.CanBeModified && map.HasPoint(point.coord) && map.GetPoint(point.coord) == point,
                $"Point not fixed or addressable: {map.GetType().Name} {point.coord} fixed={!point.CanBeModified} present={map.HasPoint(point.coord)} same={map.GetPoint(point.coord) == point}.");
            Check(point.parents.Count == (i == 0 ? 0 : 1), "Wrong parent count.");
            Check(point.Children.Count == (i == types.Length - 1 ? 0 : 1), "Branch or missing link.");
            if (i < types.Length - 1) point = point.Children.Single();
        }
        Check(ReferenceEquals(point, map.BossMapPoint), "Route did not end at the boss.");
        Check(map.startMapPoints.Single() == map.GetPoint(3, 1), "Wrong first grid point.");
    }

    public static void Run()
    {
        foreach (Type type in new[] { typeof(SolomonSea), typeof(Overgrowth), typeof(Hive), typeof(Glory), typeof(QueenBoss), typeof(AeonglassBoss), typeof(TheArchitect), typeof(DevotedSculptorWeak), typeof(KnightsElite), typeof(MechaKnightElite) })
            ModelDb.Inject(type);
        SeedWireIds();
        foreach (var target in SolomonSeaMapPatch.GetTargets().Concat(SolomonSeaFloorCountPatch.GetTargets())
                     .Concat(SolomonSeaTreasureRoomPatch.GetTargets()).Concat(SolomonSeaRunPatch.GetTargets())
                     .Concat(SolomonSeaMapRestorePatch.GetTargets()).Concat(SolomonSeaRestSiteReadyPatch.GetTargets())
                     .Concat(SolomonSeaRestSiteActIndexPatch.GetTargets()).Concat(SolomonSeaActLoadPatch.GetTargets())
                     .Concat(SolomonSeaActSerializePatch.GetTargets()))
        {
            var method = target.HarmonyMethodType == MethodType.Getter
                ? AccessTools.PropertyGetter(target.TargetType, target.MethodName)
                : AccessTools.Method(target.TargetType, target.MethodName, target.ParameterTypes);
            Check(method != null, "Missing patch target: " + target);
        }
            Check(ModContentRegistry.GetFixedPublicEntry("LaffeySpire2", typeof(SolomonSea)) == "LAFFEY_SPIRE2_ACT_SOLOMON_SEA", "Wrong localization or wire identity.");
            Check(ModelDb.Act<SolomonSea>().Index == 3 && !ModelDb.Act<SolomonSea>().AllowInRandomActList, "Fourth act must not enter the original random list.");
            foreach (bool multiplayer in new[] { false, true })
            {
                RunState state = multiplayer ? State(typeof(Ironclad), typeof(LaffeyVariantTwo)) : State(typeof(LaffeyCharacter));
                ActModel third = state.Acts[2];
                third.SetBossEncounter(ModelDb.Encounter<QueenBoss>());
                third.SetSecondBossEncounter(ModelDb.Encounter<AeonglassBoss>());
                SolomonSeaRun.EnsureAct(state);
                SolomonSeaRun.EnsureAct(state);
                Check(state.Acts.Count == 4 && ReferenceEquals(third, state.Acts[2]) && third.HasSecondBoss, "Append duplicated an act or changed the third-act double boss.");
                state.CurrentActIndex = 3;
                int displayIndex = state.CurrentActIndex;
                Check(SolomonSeaRestSiteContext.Enter(state), "Rest-site visual context did not open.");
                SolomonSeaRestSiteActIndexPatch.Postfix(state, ref displayIndex);
                Check(displayIndex == 2 && state.CurrentActIndex == 3, "Visual fallback changed actual gameplay state.");
                Task.Run(() => Check(!SolomonSeaRestSiteContext.IsActive(state), "Visual context leaked to another thread.")).GetAwaiter().GetResult();
                SolomonSeaRestSiteReadyPatch.Finalizer(true);
                displayIndex = state.CurrentActIndex;
                SolomonSeaRestSiteActIndexPatch.Postfix(state, ref displayIndex);
                Check(displayIndex == 3 && !SolomonSeaRestSiteContext.IsActive(state), "Visual fallback leaked outside initialization.");
                ActModel fourth = state.Act;
                Check(fourth.IsMutable && !fourth.HasSecondBoss && fourth.BossEncounter == SolomonSea.PlaceholderBoss, "Invalid fourth-act room set.");
                foreach (string method in new[] { nameof(ActModel.GetNumberOfRooms), nameof(ActModel.GetNumberOfFloors) })
                {
                    int count = -1;
                    Check(!SolomonSeaFloorCountPatch.Prefix(fourth, AccessTools.Method(typeof(ActModel), method), ref count), "Floor override did not apply.");
                    Check(count == (method == nameof(ActModel.GetNumberOfFloors) ? 7 : 6), "Multiplayer must not remove a room.");
                }
                ActMap map = null!;
                Check(!SolomonSeaMapPatch.Prefix(fourth, ref map), "Map override did not apply.");
                Route(map);
                Check(Packet(map).SequenceEqual(Packet(new SolomonSeaMap())), "Peer maps differ.");
                var restored = new SavedActMap(JsonSerializer.Deserialize<SerializableActMap>(
                    JsonSerializer.Serialize(SerializableActMap.FromActMap(map), JsonSerializationUtility.Options), JsonSerializationUtility.Options)!);
                SolomonSeaMapRestorePatch.Prefix(state, restored, 3);
                Route(restored);
                Check(Packet(map).SequenceEqual(Packet(restored)), "JSON changed map topology.");
                var reader = new PacketReader();
                reader.Reset(Packet(map));
                var transmitted = new SerializableActMap();
                transmitted.Deserialize(reader);
                Route(new SavedActMap(transmitted));
                Check(Packet(map).SequenceEqual(Packet(new SavedActMap(transmitted))), "Network changed map topology.");
                ActModel restoredAct = ActModel.FromSave(fourth.ToSave());
                Check(restoredAct is SolomonSea && restoredAct.BossEncounter == fourth.BossEncounter && !restoredAct.HasSecondBoss, "Act save/load lost its boss.");
                var actJson = JsonSerializer.Serialize(fourth.ToSave(), JsonSerializationUtility.Options);
                var diskAct = JsonSerializer.Deserialize<SerializableActModel>(actJson, JsonSerializationUtility.Options)!;
                Check(diskAct.SerializableRooms.EventIds == null && diskAct.SerializableRooms.NormalEncounterIds == null
                    && diskAct.SerializableRooms.EliteEncounterIds.SequenceEqual(SolomonSea.InitialEliteEncounters.Select(encounter => encounter.Id)),
                    "Native JSON omitted nonempty elites or failed to omit empty room lists.");
                SolomonSeaActLoadPatch.Prefix(diskAct);
                ActModel diskRestoredAct = ActModel.FromSave(diskAct);
                Check(diskRestoredAct is SolomonSea && diskRestoredAct.BossEncounter == fourth.BossEncounter, "Disk act restore lost its boss.");
                DiskAndNetwork(state, fourth, map);
                EliteProgress(fourth);
                state.Map = restored;
                state.AddVisitedMapCoord(restored.StartingMapPoint.coord);
                Check(restored.GetPoint(state.CurrentMapCoord!.Value)!.Children.Single().PointType == MapPointType.Shop, "Restored route cannot advance.");
                var manager = (RunManager)RuntimeHelpers.GetUninitializedObject(typeof(RunManager));
                AccessTools.PropertySetter(typeof(RunManager), "State").Invoke(manager, new object[] { state });
                AbstractRoom room = null!;
                Check(!SolomonSeaTreasureRoomPatch.Prefix(manager, RoomType.Treasure, ref room), "Treasure override did not apply.");
                Check(room is TreasureRoom, "Fourth-act treasure could not be created.");
                Check(SolomonSeaTreasureRoomPatch.Prefix(manager, RoomType.Shop, ref room), "Unrelated room creation was intercepted.");
                SolomonSeaRunPatch.Postfix(manager);
                Check(state.Acts.Count == 4, "Reload duplicated the fourth act.");
            }
            RunState vanilla = State(typeof(Ironclad));
            SolomonSeaRun.EnsureAct(vanilla);
            Check(vanilla.Acts.Count == 3, "Non-Laffey run changed.");
            RunState otherFourth = State(typeof(LaffeyCharacter));
            AccessTools.PropertySetter(typeof(RunState), nameof(RunState.Acts)).Invoke(otherFourth,
                new object[] { otherFourth.Acts.Append(ModelDb.Act<Glory>().ToMutable()).ToArray() });
            SolomonSeaRun.EnsureAct(otherFourth);
            Check(otherFourth.Acts.Count == 4 && otherFourth.Acts[3] is Glory, "Another mod's fourth act was replaced.");
            SolomonSeaRun.EnsureAct(null);
            Console.WriteLine("PASS: production prefix/postfix logic, actual game JSON/network round-trips, mutable act save/load, fixed seven-room route, skin/mixed-party eligibility, idempotent append, third-act double boss preservation, native treasure creation and Harmony target resolution.");
    }
}
'@
$trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($checks, $parseOptions))
$references = [System.Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new()
$refRoot = Get-ChildItem 'C:/Program Files/dotnet/packs/Microsoft.NETCore.App.Ref' -Directory | Where-Object Name -Like '9.*' | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
foreach ($file in Get-ChildItem (Join-Path $refRoot.FullName 'ref/net9.0') -Filter '*.dll') {
    $references.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($file.FullName))
}
$paths = @((Join-Path $GameDataDir 'sts2.dll'), (Join-Path $GameDataDir '0Harmony.dll'), (Join-Path $GameDataDir 'GodotSharp.dll'), $RitsuPath)
foreach ($path in $paths) {
    $references.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($path))
    [void][Reflection.Assembly]::LoadFrom($path)
}
$options = [Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary)
$compilation = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('LaffeySolomonSeaChecks', $trees, $references, $options)
$stream = [IO.MemoryStream]::new()
$result = $compilation.Emit($stream)
if (!$result.Success) {
    $result.Diagnostics | Where-Object Severity -EQ 'Error' | ForEach-Object { $_.ToString() }
    throw 'Solomon Sea checks did not compile.'
}
$assembly = [Reflection.Assembly]::Load($stream.ToArray())
$assembly.GetType('SolomonSeaChecks').GetMethod('Run').Invoke($null, $null)
foreach ($language in @('zhs', 'eng')) {
    $json = Get-Content (Join-Path $projectRoot "LaffeySpire2/localization/$language/acts.json") -Raw | ConvertFrom-Json -AsHashtable
    if (!$json.ContainsKey('LAFFEY_SPIRE2_ACT_SOLOMON_SEA.title')) { throw "Missing act title: $language" }
}
Write-Output 'PASS: act localization. No DLL or PCK written.'
