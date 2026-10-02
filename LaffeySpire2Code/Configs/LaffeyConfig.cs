using System.Text.Json;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace LaffeySpire2.LaffeySpire2Code.Configs;

public enum LaffeyMultiplayerModelMode
{
	手部模型,
	腿部模型
}

public sealed class LaffeyConfig
{
	public LaffeyMultiplayerModelMode 多人模式使用哪种模型 { get; set; } = LaffeyMultiplayerModelMode.手部模型;
}

public static class LaffeyConfigPage
{
	private const string DataKey = "LaffeyConfig";
	private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> Translations =
		new(StringComparer.OrdinalIgnoreCase);

	public static readonly ModSettingsValueBinding<LaffeyConfig, LaffeyMultiplayerModelMode> ModelModeBinding = new(
		MainFile.ModId, DataKey, SaveScope.Profile,
		static settings => settings.多人模式使用哪种模型,
		static (settings, value) => settings.多人模式使用哪种模型 = value);

	public static void Register()
	{
		ModDataStore.For(MainFile.ModId).Register<LaffeyConfig>(
			key: DataKey,
			fileName: "settings.json",
			scope: SaveScope.Profile,
			defaultFactory: () => new LaffeyConfig(),
			autoCreateIfMissing: true);

		RitsuLibFramework.RegisterModSettings(MainFile.ModId, page => page
			.WithTitle(Text("config.page.title", "Laffey Settings"))
			.WithModDisplayName(Text("config.modDisplayName", "Laffey"))
			.WithVisibleOnHostSurfaces(ModSettingsHostSurface.MainMenu | ModSettingsHostSurface.RunPause)
			.AddSection("general", section => section
				.WithTitle(Text("config.section.general", "General"))
				.AddChoice("mosaic_mode", Text("config.modelMode.label", "Multiplayer model"),
					ModelModeBinding,
					[
						new(LaffeyMultiplayerModelMode.手部模型, Text("config.modelMode.hand", "Hand model")),
						new(LaffeyMultiplayerModelMode.腿部模型, Text("config.modelMode.legs", "Leg model"))
					],
					presentation: ModSettingsChoicePresentation.Dropdown)));
	}

	private static ModSettingsText Text(string key, string fallback) =>
		ModSettingsText.Dynamic(() => ResolveText(key, fallback));

	private static string ResolveText(string key, string fallback)
	{
		string language = STS2RitsuLib.Utils.I18N.ResolveCurrentLanguageCode();
		if (LoadTranslations(language).TryGetValue(key, out string? text))
			return text;
		return LoadTranslations("eng").TryGetValue(key, out text) ? text : fallback;
	}

	private static IReadOnlyDictionary<string, string> LoadTranslations(string language)
	{
		if (Translations.TryGetValue(language, out IReadOnlyDictionary<string, string>? cached))
			return cached;
		string path = $"res://LaffeySpire2/localization/{language}/setting.json";
		IReadOnlyDictionary<string, string> loaded = Godot.FileAccess.FileExists(path)
			? JsonSerializer.Deserialize<Dictionary<string, string>>(Godot.FileAccess.GetFileAsString(path)) ?? []
			: new Dictionary<string, string>();
		Translations[language] = loaded;
		return loaded;
	}
}
