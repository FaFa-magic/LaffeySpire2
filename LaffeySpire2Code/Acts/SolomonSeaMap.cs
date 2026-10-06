using MegaCrit.Sts2.Core.Map;

namespace LaffeySpire2.LaffeySpire2Code.Acts;

public sealed class SolomonSeaMap : ActMap
{
	public const int RoomCount = 7;

	public override MapPoint StartingMapPoint { get; }
	public override MapPoint BossMapPoint { get; }
	protected override MapPoint?[,] Grid { get; } = new MapPoint?[7, RoomCount - 1];

	public SolomonSeaMap()
	{
		StartingMapPoint = CreatePoint(0, MapPointType.Treasure);
		MapPointType[] roomTypes =
		[
			MapPointType.Shop,
			MapPointType.RestSite,
			MapPointType.Elite,
			MapPointType.Elite,
			MapPointType.RestSite
		];
		MapPoint previous = StartingMapPoint;
		for (int i = 0; i < roomTypes.Length; i++)
		{
			MapPoint point = CreatePoint(i + 1, roomTypes[i]);
			Grid[3, i + 1] = point;
			previous.AddChildPoint(point);
			previous = point;
		}
		BossMapPoint = CreatePoint(RoomCount - 1, MapPointType.Boss);
		previous.AddChildPoint(BossMapPoint);
		startMapPoints.Add(Grid[3, 1]!);
	}

	private static MapPoint CreatePoint(int row, MapPointType type) => new(3, row)
	{
		PointType = type,
		CanBeModified = false
	};
}
