using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace LaffeySpire2.LaffeySpire2Code.Characters;

[RegisterCharacter]
public class LaffeyCharacter : ModCharacterTemplate<LaffeyCardPool, LaffeyRelicPool, LaffeyPotionPool>
{
	public const string CharacterId = "Laffey";
	public const string CharacterColor = "Laffey_blue";
	public virtual LaffeySkin CurrentSkin => LaffeySkin.Default;
	public LaffeySkinDefinition CurrentSkinDefinition => LaffeySkinManager.GetDefinition(CurrentSkin);

	public override CharacterGender Gender => CharacterGender.Feminine;
	public override int StartingHp => 70;
	public override int StartingGold => 99;

	public override Color NameColor => new("#9FDCFA");
	public override Color EnergyLabelOutlineColor => new("7BABC2");
	public override Color MapDrawingColor => new("#9FDCFA");
	public override Color DialogueColor => new("#9FDCFA");
	public override Color RemoteTargetingLineColor => new("#AAAAAA");
	public override Color RemoteTargetingLineOutline => Colors.Black;

	public override CharacterAssetProfile AssetProfile => CharacterAssetProfiles.Merge(
		CharacterAssetProfiles.Ironclad(),
		new(
			Scenes: new(
				VisualsPath: "res://LaffeySpire2/scenes/characters/Laffey.tscn",
				MerchantAnimPath: CurrentSkinDefinition.MerchantAnimPath,
				RestSiteAnimPath: CurrentSkinDefinition.RestSiteAnimPath
			),
			Spine: new(
				CombatSkeletonDataPath: CurrentSkinDefinition.SpineSkeletonDataPath
			)
		));

	public override float AttackAnimDelay => 0f;
	public override float CastAnimDelay => 0f;
	public override bool RequiresEpochAndTimeline => false;

	protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
		RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.Scenes!.VisualsPath!);

	protected override CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller) =>
		ModAnimStateMachines.Standard(
			controller,
			idleName: "normal",
			deadName: "dead",
			hitName: "touch",
			attackName: "attack",
			castName: "attack_left",
			relaxedName: "sleep");

	public override List<string> GetArchitectAttackVfx() =>
	[
		"vfx/vfx_attack_blunt",
		"vfx/vfx_heavy_blunt",
		"vfx/vfx_attack_slash",
		"vfx/vfx_bloody_impact",
		"vfx/vfx_rock_shatter"
	];
}
