using Godot;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

/// <summary>Physical traces connect the socket output contacts to the reward LEDs.</summary>
public partial class PatchworkWiringView : Control
{
	private PatchworkBoardView? _board;
	private Dictionary<int, Panel> _leds = [];
	private HashSet<int> _claimed = [], _preview = [];
	private float _time;
	public void Configure(PatchworkBoardView board, Dictionary<int, Panel> leds)
	{ _board = board; _leds = leds; MouseFilter = MouseFilterEnum.Ignore; QueueRedraw(); }
	public void Present(IEnumerable<int> claimed, IEnumerable<int> preview)
	{ _claimed = claimed.ToHashSet(); _preview = preview.ToHashSet(); QueueRedraw(); }
	public override void _Process(double delta)
	{ _time += (float)delta; if (_claimed.Count > 0 || _preview.Count > 0) QueueRedraw(); }
	public override void _Draw()
	{
		if (_board == null) return;
		Transform2D inverse = GetGlobalTransformWithCanvas().AffineInverse();
		foreach (var (size, led) in _leds)
		{
			Vector2 end = inverse * (led.GetGlobalTransformWithCanvas() * (led.Size / 2));
			Vector2 start = inverse * (_board.GetGlobalTransformWithCanvas() * _board.RewardPort(size));
			float busX = start.X - 12 - (size - 3) * 3;
			Vector2[] points = [start, new(busX, start.Y), new(busX, end.Y), end];
			bool active = _claimed.Contains(size), preview = _preview.Contains(size);
			Color color = active ? PatchworkVisuals.Cyan : preview ? PatchworkVisuals.Amber : new Color("586777");
			DrawPolyline(points, new Color("050c15"), 5, true);
			if (active || preview) DrawPolyline(points, new Color(color, 0.2f), 7, true);
			DrawPolyline(points, new Color(color, active || preview ? 0.95f : 0.55f), 1.6f, true);
			DrawCircle(start, 3, color);
			if (!active && !preview) continue;
			float length = 0;
			for (int i = 1; i < points.Length; i++) length += points[i].DistanceTo(points[i - 1]);
			float distance = (_time * 72 + size * 17) % Math.Max(1, length);
			for (int i = 1; i < points.Length; i++)
			{
				float segment = points[i].DistanceTo(points[i - 1]);
				if (distance > segment) { distance -= segment; continue; }
				Vector2 point = points[i - 1].Lerp(points[i], distance / Math.Max(1, segment));
				DrawCircle(point, 6, new Color(color, 0.18f));
				DrawCircle(point, 2.4f, color.Lightened(0.45f));
				break;
			}
		}
	}
}
