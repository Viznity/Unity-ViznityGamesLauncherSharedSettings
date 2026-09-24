using UnityEngine;

namespace Viznity.SharedSettings
{
    /// <summary>
    /// "Apply the launcher language once per change." The launcher's language should win when it changes,
    /// but a language the player then picks inside the game must be kept. So this remembers (in PlayerPrefs)
    /// what the launcher said last time and hands out a language only when that changed.
    /// <code>
    /// if (LanguageSync.TryTakeChangedLanguage(out string code)) MyLanguageMenu.Select(code);
    /// </code>
    /// </summary>
    public static class LanguageSync
    {
        public const string MarkerPref = "viznity.sharedsettings.languageApplied";

        /// <summary>Where the "last applied" marker lives. PlayerPrefs in games; replaceable in tests.</summary>
        public interface IMarkerStore
        {
            string Get();
            void Set(string marker);
        }

        private sealed class PlayerPrefsStore : IMarkerStore
        {
            private readonly string _key;
            public PlayerPrefsStore(string key) { _key = key; }
            public string Get() => PlayerPrefs.GetString(_key, string.Empty);
            public void Set(string marker) { PlayerPrefs.SetString(_key, marker); PlayerPrefs.Save(); }
        }

        /// <summary>
        /// For <see cref="ViznitySharedSettings.GameId"/> and the current file. True (with the code) only when
        /// sync is on and the launcher's language for this game changed since the last call that returned it.
        /// </summary>
        public static bool TryTakeChangedLanguage(out string languageCode)
        {
            return TryTakeChangedLanguage(ViznitySharedSettings.Current, ViznitySharedSettings.GameId, new PlayerPrefsStore(MarkerPref), out languageCode);
        }

        /// <summary>
        /// Testable core. The marker also records "sync off", so turning sync on later with the same language
        /// still counts as a change. With no launcher file, nothing is recorded (the game keeps its own choice).
        /// </summary>
        public static bool TryTakeChangedLanguage(SharedSettingsSnapshot settings, string gameId, IMarkerStore store, out string languageCode)
        {
            languageCode = null;
            if (settings == null || !settings.Exists) return false;

            bool sync = settings.GameLanguageSyncEnabled;
            string perGame = gameId != null && settings.GameLanguages.TryGetValue(gameId, out string g) ? g : null;
            string wanted = perGame ?? settings.LauncherLanguage ?? string.Empty;
            string marker = (sync ? "on:" : "off:") + wanted;
            if (store.Get() == marker) return false;
            store.Set(marker);

            languageCode = settings.LanguageFor(gameId);
            return languageCode != null;
        }

        /// <summary>"pt-BR" → "pt", "zh_Hans" → "zh" (lower case). For games that ship one variant per language.</summary>
        public static string PrimarySubtag(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode)) return null;
            int cut = languageCode.IndexOfAny(new[] { '-', '_' });
            return (cut < 0 ? languageCode : languageCode.Substring(0, cut)).ToLowerInvariant();
        }
    }
}
