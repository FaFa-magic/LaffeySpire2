using System.Threading;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using STS2RitsuLib.Patching.Models;

namespace LaffeySpire2.LaffeySpire2Code.Patches;

public sealed class LaffeyDreamTransitionPatch : IPatchMethod
{
	public const string TransitionPath = "res://LaffeySpire2/materials/laffey_dream_transition_mat.tres";
	public const float Duration = 4.5f;

	public static string PatchId => "laffey_sleep_to_dream_transition";
	public static string Description => "Play Laffey's sleeping and dream character-select transition";
	public static bool IsCritical => false;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NTransition), nameof(NTransition.FadeOut),
			[typeof(float), typeof(string), typeof(CancellationToken?)])
	];

	[HarmonyPrefix]
	public static void Prefix(NTransition __instance, ref float time, string transitionPath,
		CancellationToken? cancelToken, out HiddenOverlays? __state)
	{
		__state = null;
		if (transitionPath != TransitionPath || !float.IsFinite(time) || time <= 0f
			|| cancelToken?.IsCancellationRequested == true
			|| SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant)
			return;

		time = Duration;
		__state = new HiddenOverlays(__instance.GetNodeOrNull<Control>("SimpleTransition"),
			__instance.GetNodeOrNull<Control>("GradientTransition"));
	}

	[HarmonyPostfix]
	public static void Postfix(ref Task __result, HiddenOverlays? __state)
	{
		if (__state is not null)
			__result = RestoreAfterTransition(__result, __state);
	}

	[HarmonyFinalizer]
	public static Exception? Finalizer(Exception? __exception, HiddenOverlays? __state)
	{
		if (__exception is not null)
			__state?.Restore();
		return __exception;
	}

	private static async Task RestoreAfterTransition(Task transition, HiddenOverlays overlays)
	{
		try { await transition; }
		finally { overlays.Restore(); }
	}

	public sealed class HiddenOverlays
	{
		private readonly Control? _simple;
		private readonly Control? _gradient;
		private readonly bool _simpleVisible;
		private readonly bool _gradientVisible;
		private bool _restored;

		public HiddenOverlays(Control? simple, Control? gradient)
		{
			_simple = simple;
			_gradient = gradient;
			if (GodotObject.IsInstanceValid(simple))
			{
				_simpleVisible = simple!.Visible;
				simple.Visible = false;
			}
			if (GodotObject.IsInstanceValid(gradient))
			{
				_gradientVisible = gradient!.Visible;
				gradient.Visible = false;
			}
		}

		public void Restore()
		{
			if (_restored) return;
			_restored = true;
			if (GodotObject.IsInstanceValid(_simple)) _simple!.Visible = _simpleVisible;
			if (GodotObject.IsInstanceValid(_gradient)) _gradient!.Visible = _gradientVisible;
		}
	}
}
