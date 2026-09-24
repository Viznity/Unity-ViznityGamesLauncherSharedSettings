using System;
using UnityEngine;

namespace Viznity.SharedSettings
{
    /// <summary>
    /// Applies <see cref="ViznitySharedSettingsConfig"/> before the first scene loads: sets the game id,
    /// carries an old marker over, writes the language option (PlayerPrefsIndex) and starts the watcher.
    /// The Unity Localization target is applied by that optional module's own bootstrap.
    /// Never throws: a launcher setting must not break a game's startup.
    /// </summary>
    public static class SharedSettingsBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Run()
        {
            try
            {
                ViznitySharedSettingsConfig config = ViznitySharedSettingsConfig.Load();
                if (config == null) return;
                Apply(config);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Viznity Shared Settings] Skipped: " + e.Message);
            }
        }

        /// <summary>Everything the config asks for except the Unity Localization target.</summary>
        public static void Apply(ViznitySharedSettingsConfig config)
        {
            if (!string.IsNullOrEmpty(config.gameId)) ViznitySharedSettings.GameId = config.gameId;
            MigrateLegacyMarker(config.legacyMarkerPrefKey);

            // Index targets with an explicit code list are applied here; with no list the index comes from
            // Unity Localization's locales, which the Localization module applies once they are loaded.
            bool indexTarget = config.languageTarget == ViznitySharedSettingsConfig.LanguageTarget.PlayerPrefsIndex ||
                               config.languageTarget == ViznitySharedSettingsConfig.LanguageTarget.GameSettingsStore;
            if (indexTarget && !config.UsesLocaleOrder && LanguageSync.TryTakeChangedLanguage(out string code))
            {
                int index = config.IndexOf(code);
                if (index >= 0) WriteLanguageIndex(config, code, index);
                else Debug.Log($"[Viznity Shared Settings] This game has no '{code}' option; keeping its own language.");
            }
            else if (indexTarget && config.UsesLocaleOrder &&
                     GameSettingsStore.FindType("Viznity.SharedSettings.Localization.UnityLocalizationLanguageSync") == null)
            {
                Debug.LogWarning("[Viznity Shared Settings] Language Codes is empty and Unity Localization is not installed, so there is no option order to use. Fill in Language Codes.");
            }

            if (config.watchForChanges) ViznitySharedSettings.WatchForChanges = true;
        }

        /// <summary>
        /// Writes <paramref name="index"/> where the config's target keeps the language: a PlayerPrefs int, or the
        /// game's own settings store (then saved and re-applied through the game's methods). Returns false when the
        /// target could not be written; the reason is logged.
        /// </summary>
        public static bool WriteLanguageIndex(ViznitySharedSettingsConfig config, string code, int index)
        {
            switch (config.languageTarget)
            {
                case ViznitySharedSettingsConfig.LanguageTarget.PlayerPrefsIndex:
                    if (string.IsNullOrEmpty(config.playerPrefsKey)) return false;
                    PlayerPrefs.SetInt(config.playerPrefsKey, index);
                    PlayerPrefs.Save();
                    break;
                case ViznitySharedSettingsConfig.LanguageTarget.GameSettingsStore:
                    if (!GameSettingsStore.Write(config, index)) return false;
                    break;
                default:
                    return false;
            }
            Debug.Log($"[Viznity Shared Settings] Using the launcher language '{code}' (option {index}).");
            return true;
        }

        private static void MigrateLegacyMarker(string legacyKey)
        {
            if (string.IsNullOrEmpty(legacyKey) || !PlayerPrefs.HasKey(legacyKey)) return;
            if (!PlayerPrefs.HasKey(LanguageSync.MarkerPref)) PlayerPrefs.SetString(LanguageSync.MarkerPref, PlayerPrefs.GetString(legacyKey, string.Empty));
            PlayerPrefs.DeleteKey(legacyKey);
            PlayerPrefs.Save();
        }
    }
}
