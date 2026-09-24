# Viznity Shared Settings for Unity

`com.viznitygames.sharedsettings` lets a Unity game read the settings that **Viznity Games Desktop** (the launcher) shares with every installed game: the language, a language picked for one game, "skip game intros", achievement notifications, and any setting the launcher adds later.

- **No dependencies.** It works in any Unity 2021.3+ project and reads plain JSON.
- **Safe by default.** A missing, locked or broken file means "use the defaults", so a game behaves the same when the launcher is not installed.
- **Future-proof.** Keys added by newer launchers can be read without a new package version.
- **Windows, macOS and Linux.** It reads the same folder the launcher writes to.
- **Optional Unity Localization support**, compiled only when `com.unity.localization` is in the project.

---

## Contents

1. [Install](#1-install)
2. [Quick start](#2-quick-start)
3. [The settings](#3-the-settings)
4. [Language](#4-language)
5. [Skip intro](#5-skip-intro)
6. [Reacting to changes while the game runs](#6-reacting-to-changes-while-the-game-runs)
7. [Reading any key](#7-reading-any-key)
8. [Adding a new setting to the launcher](#8-adding-a-new-setting-to-the-launcher)
9. [Do I have to update every game?](#9-do-i-have-to-update-every-game)
10. [The file contract](#10-the-file-contract)
11. [Tests](#11-tests)

---

## 1. Install

In **Window › Package Manager › + › Add package from git URL…**, enter:

```
https://github.com/Viznity/Unity-ViznityGamesLauncherSharedSettings.git#v1.3.0
```

You can also add it to `Packages/manifest.json`:

```json
"com.viznitygames.sharedsettings": "https://github.com/Viznity/Unity-ViznityGamesLauncherSharedSettings.git#v1.3.0"
```

Pin a tag (`#v1.3.0`). Every game then keeps the exact version it was tested with, until you move it to a newer tag yourself.

## 2. Quick start

Every game is set up with **one asset and no script**:

1. Open **Tools › Viznity › Shared Settings › Create or Select Config**. This creates
   `Assets/Resources/ViznitySharedSettingsConfig.asset`.
2. In the Inspector, set:
   - **Game Id**: leave it empty. The id is derived from Project Settings › Product Name
     (`Rick's Lewd Universe` → `ricks-lewd-universe`, `Hellasure` → `hellasure`), which matches the
     launcher's `releases::GAME_IDS`. Fill it in only if a product name ever differs from the launcher id.
   - **Language Target**: where the launcher's language goes (see §4):
     - `UnityLocalization`: select the matching locale in Unity Localization.
     - `PlayerPrefsIndex`: for games whose language menu stores an option index in PlayerPrefs.
       Fill in **Player Prefs Key** and **Language Codes** in menu order.
     - `GameSettingsStore`: for games with their own settings system (Hellasure). The package
       writes the option index into the game's store by name and asks the game to apply it.
     - `None`: do nothing with the language.
   - **Watch For Changes** (optional): react while the game runs (§6).

Before the first scene loads, the package reads the config and applies it. A game without the
asset is left alone, and the code API below still works.

Read other settings wherever you need them:

```csharp
using Viznity.SharedSettings;

if (ViznitySharedSettings.ShouldSkipIntro) LoadMainMenu();
bool toasts = ViznitySharedSettings.Current.AchievementNotificationsEnabled;
```

**Tools › Viznity › Shared Settings › Show Current File** logs the file the package reads and
opens its folder.

## 3. The settings

| Key | Type | Default | API |
|---|---|---|---|
| `schema_version` | number | 0 | `Current.SchemaVersion` |
| `achievement_notifications_enabled` | bool | `true` | `Current.AchievementNotificationsEnabled` |
| `game_language_sync_enabled` | bool | `false` | `Current.GameLanguageSyncEnabled` |
| `launcher_language` | string (`"en"`, `"tr"`, `"pt-BR"`) | none | `Current.LauncherLanguage` |
| `game_languages` | object `{ "<game id>": "<code>" }` | empty | `Current.GameLanguages`, `Current.LanguageFor(id)` |
| `skip_intro` | bool | `false` | `Current.SkipIntro`, `ViznitySharedSettings.ShouldSkipIntro` |

The key names are constants in `SharedSettingKeys`.

The file lives in the per-user config folder:

| OS | Path |
|---|---|
| Windows | `%APPDATA%\Viznity Games\shared-settings.json` |
| macOS | `~/Library/Application Support/Viznity Games/shared-settings.json` |
| Linux | `$XDG_CONFIG_HOME/Viznity Games/shared-settings.json`, or `~/.config/Viznity Games/...` |

`ViznitySharedSettings.FilePath` returns the path for the current machine. You can also set it, for example to point a test at a fixture file.

## 4. Language

`Current.LanguageFor(gameId)` decides which language the launcher wants for a game:

1. When **language sync is off**, it returns `null`. The game keeps its own language.
2. When a language was **picked for this game** in the launcher (`game_languages`), it returns that one.
3. Otherwise it returns the **launcher's own language**.

The launcher's language should win when it changes, but a language the player then picks inside the game has to stay. `LanguageSync` handles this by returning a language only when the launcher's choice changed since the last time it applied one:

```csharp
if (LanguageSync.TryTakeChangedLanguage(out string code))
    MyLanguageMenu.Select(code);   // "pt-BR" etc.; use LanguageSync.PrimarySubtag(code) for "pt"
```

**Unity Localization.** Set **Language Target** to `UnityLocalization`. The package waits for
Localization to initialize, then selects the matching locale: an exact match (`pt-BR`) first, then
the same language (`pt`). If the game doesn't ship the language, nothing changes. This part is its
own assembly and compiles only when `com.unity.localization` 1.0+ is installed, so games without
Localization are unaffected. From code, call `UnityLocalizationLanguageSync.ApplyWhenReady()`.

**Option-index language menus.** Some games keep their own language menu that stores an index in
PlayerPrefs and applies it in `Start` (for example Rick's Lewd Universe's `ChangeLanguage.cs`, which
also drives Dialogue System and Localization). Set **Language Target** to `PlayerPrefsIndex`,
**Player Prefs Key** to that key (`currentOption`), and **Language Codes** in menu order, e.g.
`en, ru, tr, fr, it, de, es, pt, ja, ko, zh`. The index is written before the first scene, so the
menu's `Start` already sees it. **Legacy Marker Pref Key** carries over the marker of an older
per-game script once (Rick's: `launcherLanguageApplied`), so players keep their in-game choice.

**Games with their own settings system (Hellasure).** Hellasure keeps its settings in
`Game.UI.SettingsSaveManager` (a `Settings.json` in `persistentDataPath`). The language is an index
into Unity Localization's locales under `OptionPicker_Language` and `OptionPicker_DialogueLanguage`,
and `Game.UI.SettingsBootstrap` applies it at start (Localization, Dialogue System). If the package
set the locale directly, that bootstrap would put the saved one back. So the config writes into the
store instead:

| Field | Hellasure |
|---|---|
| Language Target | `GameSettingsStore` |
| Language Codes | empty (use Unity Localization's locale order, like the game's menu) |
| Settings Store Type | `Game.UI.SettingsSaveManager` |
| Settings Store Set Int Method | `SetInt` |
| Settings Store Save Method | `SaveSettings` |
| Settings Store Keys | `OptionPicker_Language`, `OptionPicker_DialogueLanguage` |
| Reapply Method | `Game.UI.SettingsBootstrap.ReloadAndApply` |

The methods are found by name at runtime, so the package has no compile-time dependency on the
game. A wrong name is logged as a warning and the game keeps its language. The launcher language is
applied once per change, so a language the player then picks in the settings menu is kept.

**Dialogue System** or other systems: call `TryTakeChangedLanguage` and pass the code to the system, e.g. `DialogueManager.SetLanguage(code)`.

## 5. Skip intro

When the player turns on **Skip game intros**, the launcher adds `-nointro` to the game's launch arguments. It also writes `skip_intro`, which covers games started without the launcher (a desktop shortcut, Steam):

```csharp
if (ViznitySharedSettings.ShouldSkipIntro) SceneManager.LoadScene("MainMenu");
```

`ShouldSkipIntro` is `LaunchedWithNoIntro || Current.SkipIntro`. If you only want the command-line flag, use `CommandLine.HasFlag("-nointro")`.

## 6. Reacting to changes while the game runs

Most settings only need to be read at startup. If a game should also react while it runs, for example when the player changes a setting in the launcher with the game open:

```csharp
ViznitySharedSettings.WatchForChanges = true;
ViznitySharedSettings.Changed += settings =>
{
    AchievementToasts.Enabled = settings.AchievementNotificationsEnabled;
};
```

The watcher checks the file's timestamp and size every 2 seconds (`SharedSettingsWatcher.IntervalSeconds`) and again when the game regains focus. It reads the file only when those changed. `Changed` is raised on the main thread, so handlers can touch Unity objects. The watcher polls instead of using `FileSystemWatcher` because that is unreliable under Mono on macOS.

To check once at a moment you choose, call `ViznitySharedSettings.ReloadIfFileChanged()` or `ViznitySharedSettings.Reload()`.

## 7. Reading any key

A game can read settings that this package version does not know yet:

```csharp
var s = ViznitySharedSettings.Current;
bool hdr      = s.GetBool("graphics_hdr", false);
int  fpsLimit = s.GetInt("fps_limit", 0);
string theme  = s.GetString("ui_theme", "dark");
if (s.TryGetObject("audio", out var audio)) { double music = (double)audio["music"]; }
```

JSON numbers are `double`, and wrong types are never coerced: `GetBool` on a string returns the fallback. `s.Keys` lists everything in the file.

## 8. Adding a new setting to the launcher

The example below adds `subtitles_enabled` (bool, default on).

**Launcher (`ViznityDesktopApp`)**

1. `src-tauri/src/shared_settings.rs`: add `pub const SUBTITLES_ENABLED: &str = "subtitles_enabled";` with a doc comment for its type and default.
2. In the same file, add a `#[tauri::command]` that validates the value and calls `write_setting(&path, SUBTITLES_ENABLED, json!(value))`. Copy `set_shared_game_options` as a model. Register the command in `lib.rs`. `write_setting` keeps every other key and writes atomically.
3. Add a test next to the existing ones in `shared_settings::tests`.
4. Frontend: add the setting to `src/types/settings.ts` and to the defaults in `mockData`, write it through `src/lib/settings/sharedSettings.ts`, and add the control in Settings › *Shared Game Settings*. Add its text keys to `src/i18n/locales/en.json`.
5. Mirror the value on startup as well as on change, so the file is correct even for players who never open Settings.

**This package (optional but recommended)**

6. Add `public const string SubtitlesEnabled = "subtitles_enabled";` to `SharedSettingKeys`.
7. Add a typed property to `SharedSettingsSnapshot`: `public bool SubtitlesEnabled => GetBool(SharedSettingKeys.SubtitlesEnabled, true);`.
8. Add a test, update this README's table and `CHANGELOG.md`, bump `version` in `package.json`, then tag the commit: `git tag v1.1.0 && git push --tags`.

**Each game that should honor it**

9. Use the setting, e.g. `ViznitySharedSettings.Current.SubtitlesEnabled`. Before steps 6–8 are released, `Current.GetBool("subtitles_enabled", true)` works too.
10. If you did steps 6–8, move the game's manifest entry to the new tag (`#v1.3.0`).
11. Build and publish a new game version.

Rules for new keys:
- Use `snake_case` names and never reuse or change the meaning of a key.
- Choose defaults that match the behavior of a game with no launcher. A missing key must behave like today.
- To retire a setting, stop writing it but leave readers tolerant. Never delete other keys from the file.
- If a value's meaning has to change, add a new key instead and bump `schema_version` in the launcher (`SCHEMA_VERSION`).

## 9. Do I have to update every game?

It depends on who acts on the setting:

- **The launcher alone** (it passes an argument or changes how it launches the game): no game update. `skip_intro` works this way for games that already understand `-nointro`.
- **The game** (it shows or hides something, changes a system): yes, but only the games that should honor it. The code that uses the setting ships inside the game, so each of those games needs a new build. Games that don't care simply ignore the key, and nothing breaks.
- **The package only**: you do not need a new package version just to *read* a new key, because `GetBool`/`GetString`/... read any key. A new package version is only needed for a new typed property, or for behavior the package itself should perform (like the Unity Localization sync). In that case each game moves to the new tag and is rebuilt.

In short, adding a setting never breaks existing games. Only games that should *react* to it need a new build.

## 10. The file contract

The launcher and this package agree on the following:

- The file is one JSON object in UTF-8. Unknown keys are allowed and preserved.
- The launcher writes atomically: it writes a temp file in the same folder and renames it over the original. Readers see the whole old file or the whole new file. The package opens it with shared read access, so a read never blocks the launcher's rename.
- Files over 256 KB, malformed JSON, and anything that isn't an object are treated as "no file" (defaults).
- The package only reads. Games must never write this file.
- Language codes follow the launcher's rule: letters, digits and `-`, 2–12 characters. Invalid entries in `game_languages` are ignored.

## 11. Tests

EditMode tests live in `Tests/Editor`. To run them from a game project, add the package to `testables` in `Packages/manifest.json`:

```json
"testables": ["com.viznitygames.sharedsettings"]
```

Then open **Window › General › Test Runner › EditMode**.

## License

MIT, see [LICENSE.md](LICENSE.md).
