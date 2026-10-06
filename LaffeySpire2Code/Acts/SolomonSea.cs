using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.Acts;

[RegisterAct]
public sealed class SolomonSea : ModActTemplate
{
	private static Glory VanillaAct => ModelDb.Act<Glory>();

	public static EncounterModel PlaceholderBoss => ModelDb.Encounter<QueenBoss>();
	public static IEnumerable<EncounterModel> InitialEliteEncounters =>
	[
		ModelDb.Encounter<KnightsElite>(),
		ModelDb.Encounter<MechaKnightElite>()
	];

	public override int Index => 3;
	public override bool IsDefault => false;
	protected override int BaseNumberOfRooms => SolomonSeaMap.RoomCount - 1;
	protected override int NumberOfWeakEncounters => 0;
	public override Color MapTraveledColor => VanillaAct.MapTraveledColor;
	public override Color MapUntraveledColor => VanillaAct.MapUntraveledColor;
	public override Color MapBgColor => VanillaAct.MapBgColor;
	public override string[] BgMusicOptions => VanillaAct.BgMusicOptions;
	public override string[] MusicBankPaths => VanillaAct.MusicBankPaths;
	public override string AmbientSfx => VanillaAct.AmbientSfx;
	public override string ChestSpineSkinNameNormal => VanillaAct.ChestSpineSkinNameNormal;
	public override string ChestSpineSkinNameStroke => VanillaAct.ChestSpineSkinNameStroke;
	public override string ChestOpenSfx => VanillaAct.ChestOpenSfx;
	public override ActAssetProfile AssetProfile => ContentAssetProfiles.FromVanillaActId("glory") with
	{
		ChestSpineResourcePath = VanillaAct.ChestSpineResourcePath
	};
	public override IEnumerable<EncounterModel> BossDiscoveryOrder => [PlaceholderBoss];
	public override IEnumerable<AncientEventModel> AllAncients => [];
	public override IEnumerable<EventModel> AllEvents => [];

	public override IEnumerable<EncounterModel> GenerateAllEncounters() => [.. InitialEliteEncounters, PlaceholderBoss];
	public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState unlockState) => [];
	public override bool IsUnlocked(UnlockState unlockState) => true;
	public override MapPointTypeCounts GetMapPointTypes(Rng mapRng) => new(0, 2) { NumOfElites = 2 };

	protected override void ApplyActDiscoveryOrderModifications(UnlockState unlockState)
	{
	}

	public static ActModel CreateForRun()
	{
		SolomonSea act = (SolomonSea)ModelDb.Act<SolomonSea>().ToMutable();
		act._rooms.eliteEncounters.AddRange(InitialEliteEncounters);
		act.SetBossEncounter(PlaceholderBoss);
		return act;
	}
}
