using System;
using System.Collections.Generic;
using System.Globalization;

namespace Viznity.SharedSettings
{
    /// <summary>
    /// One reading of <c>shared-settings.json</c>. Immutable: call <see cref="ViznitySharedSettings.Reload"/>
    /// for a new one. A missing or unreadable file gives <see cref="Empty"/>, where every setting has its default,
    /// so a game never behaves differently just because the launcher is not installed.
    /// </summary>
    public sealed class SharedSettingsSnapshot
    {
        private static readonly IReadOnlyDictionary<string, object> NoValues = new Dictionary<string, object>(StringComparer.Ordinal);

        /// <summary>No file (or an unreadable one): every setting at its default.</summary>
        public static readonly SharedSettingsSnapshot Empty = new SharedSettingsSnapshot(NoValues, false);

        private readonly IReadOnlyDictionary<string, object> _values;

        private SharedSettingsSnapshot(IReadOnlyDictionary<string, object> values, bool exists)
        {
            _values = values;
            Exists = exists;
            GameLanguages = ReadGameLanguages(values);
        }

        /// <summary>Parses the file's text. Returns <see cref="Empty"/> for text that is not a JSON object.</summary>
        public static SharedSettingsSnapshot FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return Empty;
            try
            {
                return SharedSettingsJson.Parse(json) is Dictionary<string, object> values
                    ? new SharedSettingsSnapshot(values, true)
                    : Empty;
            }
            catch (FormatException)
            {
                // The launcher replaces a malformed file on its next write; until then, defaults.
                return Empty;
            }
        }

        /// <summary>The file existed and was a JSON object.</summary>
        public bool Exists { get; }

        /// <summary>Every key in the file, including ones this package version does not know.</summary>
        public IEnumerable<string> Keys => _values.Keys;

        // ----- Settings the launcher writes today (SharedSettingKeys) -----

        public int SchemaVersion => TryGetNumber(SharedSettingKeys.SchemaVersion, out double v) ? (int)v : 0;

        /// <summary>Show achievement unlock toasts. Default true.</summary>
        public bool AchievementNotificationsEnabled => GetBool(SharedSettingKeys.AchievementNotificationsEnabled, true);

        /// <summary>Use the launcher's language in games. Default false.</summary>
        public bool GameLanguageSyncEnabled => GetBool(SharedSettingKeys.GameLanguageSyncEnabled, false);

        /// <summary>The launcher's UI language ("en", "tr", "pt-BR"), or null.</summary>
        public string LauncherLanguage => GetString(SharedSettingKeys.LauncherLanguage, null);

        /// <summary>Languages picked per game in the launcher, by game id. Empty when none.</summary>
        public IReadOnlyDictionary<string, string> GameLanguages { get; }

        /// <summary>Skip the studio intro. Default false. See also <see cref="ViznitySharedSettings.ShouldSkipIntro"/>.</summary>
        public bool SkipIntro => GetBool(SharedSettingKeys.SkipIntro, false);

        /// <summary>
        /// The language the launcher wants for <paramref name="gameId"/>: the per-game choice, else the launcher's
        /// own language. Null when language sync is off or no language is set, which means "keep the game's own choice".
        /// </summary>
        public string LanguageFor(string gameId)
        {
            if (!GameLanguageSyncEnabled) return null;
            if (!string.IsNullOrEmpty(gameId) && GameLanguages.TryGetValue(gameId, out string perGame) && IsLanguageCode(perGame)) return perGame;
            string launcher = LauncherLanguage;
            return IsLanguageCode(launcher) ? launcher : null;
        }

        // ----- Any key, including ones added to the launcher after this package version -----

        public bool Has(string key) => key != null && _values.ContainsKey(key);

        /// <summary>The raw value: string, bool, double, null, <c>Dictionary&lt;string, object&gt;</c> or <c>List&lt;object&gt;</c>.</summary>
        public bool TryGetValue(string key, out object value)
        {
            value = null;
            return key != null && _values.TryGetValue(key, out value);
        }

        public bool TryGetBool(string key, out bool value)
        {
            value = false;
            if (!TryGetValue(key, out object raw) || !(raw is bool b)) return false;
            value = b;
            return true;
        }

        public bool TryGetString(string key, out string value)
        {
            value = null;
            if (!TryGetValue(key, out object raw) || !(raw is string s)) return false;
            value = s;
            return true;
        }

        public bool TryGetNumber(string key, out double value)
        {
            value = 0;
            if (!TryGetValue(key, out object raw) || !(raw is double d)) return false;
            value = d;
            return true;
        }

        /// <summary>A nested object, e.g. a future <c>{ "graphics": { ... } }</c> setting.</summary>
        public bool TryGetObject(string key, out IReadOnlyDictionary<string, object> value)
        {
            value = null;
            if (!TryGetValue(key, out object raw) || !(raw is Dictionary<string, object> o)) return false;
            value = o;
            return true;
        }

        public bool GetBool(string key, bool fallback) => TryGetBool(key, out bool v) ? v : fallback;
        public string GetString(string key, string fallback) => TryGetString(key, out string v) ? v : fallback;
        public double GetNumber(string key, double fallback) => TryGetNumber(key, out double v) ? v : fallback;
        public int GetInt(string key, int fallback) => TryGetNumber(key, out double v) && v >= int.MinValue && v <= int.MaxValue ? (int)Math.Round(v) : fallback;

        public override string ToString()
        {
            if (!Exists) return "SharedSettings(no file)";
            var parts = new List<string>();
            foreach (var pair in _values) parts.Add(pair.Key + "=" + Describe(pair.Value));
            return "SharedSettings(" + string.Join(", ", parts) + ")";
        }

        /// <summary>The launcher's rule: letters, digits and '-', 2 to 12 characters, not starting or ending with '-'.</summary>
        public static bool IsLanguageCode(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length < 2 || code.Length > 12) return false;
            if (code[0] == '-' || code[code.Length - 1] == '-') return false;
            foreach (char c in code)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-';
                if (!ok) return false;
            }
            return true;
        }

        private static IReadOnlyDictionary<string, string> ReadGameLanguages(IReadOnlyDictionary<string, object> values)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (values.TryGetValue(SharedSettingKeys.GameLanguages, out object raw) && raw is Dictionary<string, object> map)
            {
                foreach (var pair in map)
                {
                    if (pair.Value is string code && IsLanguageCode(code)) result[pair.Key] = code;
                }
            }
            return result;
        }

        private static string Describe(object value)
        {
            switch (value)
            {
                case null: return "null";
                case string s: return "\"" + s + "\"";
                case bool b: return b ? "true" : "false";
                case double d: return d.ToString(CultureInfo.InvariantCulture);
                case Dictionary<string, object> o: return "{" + o.Count + " keys}";
                case List<object> a: return "[" + a.Count + " items]";
                default: return value.ToString();
            }
        }
    }
}
