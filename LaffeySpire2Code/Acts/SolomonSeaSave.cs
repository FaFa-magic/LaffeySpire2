using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace LaffeySpire2.LaffeySpire2Code.Acts;

public static class SolomonSeaSave
{
	public static void NormalizeRoomLists(SerializableActModel save)
	{
		if (save.Id != ModelDb.GetId<SolomonSea>() || save.SerializableRooms is not { } rooms)
			return;

		rooms.EventIds ??= [];
		rooms.NormalEncounterIds ??= [];
		rooms.EliteEncounterIds ??= [];
		if (rooms.EliteEncounterIds.Count == 0
			&& (save.SavedMap == null || save.SavedMap.Points.Any(point => point.PointType == MapPointType.Elite)))
			rooms.EliteEncounterIds.AddRange(SolomonSea.InitialEliteEncounters.Select(encounter => encounter.Id));
	}
}
