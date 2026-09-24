# Changelog

## [1.0.0] - 2026-09-24

- First release: reads Viznity Games Desktop's `shared-settings.json` on Windows, macOS and Linux.
- Typed settings: achievement notifications, language sync, launcher language, per-game languages, skip intro.
- Generic getters for any key a newer launcher adds (`TryGetBool/String/Number/Object`).
- `LanguageSync`: hands out the launcher language once per change, so the player's in-game choice is kept.
- `-nointro` detection and `ShouldSkipIntro`.
- Optional change watcher (`WatchForChanges`) with a `Changed` event on the main thread.
- Optional Unity Localization module, compiled only when `com.unity.localization` is installed.
- Sample: option-index language menus (Rick's Lewd Universe).
