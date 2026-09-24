# Changelog

## [1.2.0] - 2026-09-24

- New language target `GameSettingsStore`: writes the option index into a game's own settings store
  by name (static `SetInt(string, int)`, a save method, then a re-apply method), so games with their
  own settings system (Hellasure's `Game.UI.SettingsSaveManager`) apply the launcher language through
  their own code and show it in their settings menu. No compile-time dependency on game code.
- `PlayerPrefsIndex` and `GameSettingsStore` can take the option order from Unity Localization's
  locales when Language Codes is left empty.

## [1.1.0] - 2026-09-24

- Per-game setup without a script: `ViznitySharedSettingsConfig` asset in `Assets/Resources`
  (Tools › Viznity › Shared Settings › Create or Select Config). It sets the game id and applies the
  launcher language to Unity Localization or to a menu's PlayerPrefs option index.
- Fixed: the Unity Localization module did not compile (CS0012, missing `Unity.ResourceManager` reference).
- Removed the "Option-index language menu" sample; the config's PlayerPrefsIndex target replaces it.
- Tools › Viznity › Shared Settings › Show Current File logs and reveals the file being read.

## [1.0.0] - 2026-09-24

- First release: reads Viznity Games Desktop's `shared-settings.json` on Windows, macOS and Linux.
- Typed settings: achievement notifications, language sync, launcher language, per-game languages, skip intro.
- Generic getters for any key a newer launcher adds (`TryGetBool/String/Number/Object`).
- `LanguageSync`: hands out the launcher language once per change, so the player's in-game choice is kept.
- `-nointro` detection and `ShouldSkipIntro`.
- Optional change watcher (`WatchForChanges`) with a `Changed` event on the main thread.
- Optional Unity Localization module, compiled only when `com.unity.localization` is installed.
- Sample: option-index language menus (Rick's Lewd Universe).
