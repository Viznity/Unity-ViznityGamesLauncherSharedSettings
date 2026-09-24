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

            if (config.languageTarget == ViznitySharedSettingsConfig.LanguageTarget.PlayerPrefsIndex &&
                LanguageSync.TryTakeChangedLanguage(out string code))
            {
                int index = config.IndexOf(code);
                if (index >= 0 && !string.IsNullOrEmpty(config.playerPrefsKey))
                {
                    PlayerPrefs.SetInt(config.playerPrefsKey, index);
                    PlayerPrefs.Save();
                    Debug.Log($"[Viznity Shared Settings] Using the launcher language '{code}'.");
                }
                else
                {
                    Debug.Log($"[Viznity Shared Settings] This game has no '{code}' option; keeping its own language.");
                }
            }

            if (config.watchForChanges) ViznitySharedSettings.WatchForChanges = true;
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
