using Godot;

namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

public partial class PatchworkRewardLed : Control
{
	private bool _claimed, _preview;
	private float _time;
	public Vector2 ConnectionPoint => Size / 2 + new Vector2(13, 0);

	public void Present(bool claimed, bool preview)
	{
		_claimed = claimed; _preview = preview;
		SetMeta("claimed", claimed); SetMeta("preview", preview);
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		_time += (float)delta;
		if (_preview) QueueRedraw();
	}

	public override void _Draw()
	{
		Vector2 center = Size / 2;
		Color color = _claimed ? PatchworkVisuals.Cyan : _preview ? PatchworkVisuals.Amber : new Color("345061");
		float pulse = _preview ? 0.75f + 0.25f * MathF.Sin(_time * 3) : 1;
		if (_claimed || _preview)
			for (int radius = 18; radius >= 9; radius -= 3)
				DrawCircle(center, radius, new Color(color, 0.07f * pulse));
		DrawCircle(center, 8, new Color("15252d"));
		DrawCircle(center, 7, color.Darkened(_claimed || _preview ? 0.05f : 0.2f));
		DrawCircle(center + new Vector2(0, 1.5f), 5, new Color(color.Lightened(0.3f), 0.7f * pulse));
		DrawArc(center, 7, 0, MathF.Tau, 32, new Color("b7d5dd"), 0.8f, true);
		DrawCircle(center + new Vector2(-2, -2.5f), 2, new Color(Colors.White, 0.8f));
		DrawArc(center + new Vector2(0, -0.5f), 5, MathF.PI * 1.1f, MathF.PI * 1.7f, 12, new Color(Colors.White, 0.45f), 1, true);
	}
}
