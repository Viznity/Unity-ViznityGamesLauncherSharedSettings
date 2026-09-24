using System.Collections.Generic;
using UnityEngine;

namespace Viznity.SharedSettings
{
    /// <summary>
    /// Per-game setup, so a game needs no script of its own: one asset at
    /// <c>Assets/Resources/ViznitySharedSettingsConfig.asset</c> (Tools › Viznity Games › Shared Settings ›
    /// Create Config). Read before the first scene loads by <see cref="SharedSettingsBootstrap"/>.
    /// Without the asset the package does nothing on its own; the code API still works.
    /// </summary>
    [CreateAssetMenu(fileName = ResourceName, menuName = "Viznity Games/Shared Settings Config")]
    public sealed class ViznitySharedSettingsConfig : ScriptableObject
    {
        /// <summary>The asset's name under a Resources folder.</summary>
        public const string ResourceName = "ViznitySharedSettingsConfig";

        public enum LanguageTarget
        {
            /// <summary>Do not touch the game's language (read it yourself through the API).</summary>
            None,
            /// <summary>Select the matching locale in Unity Localization (needs com.unity.localization).</summary>
            UnityLocalization,
            /// <summary>
            /// Write an option index to a PlayerPrefs int that the game's own language menu reads at start
            /// (the index is the position of the language in <see cref="languageCodes"/>).
            /// </summary>
            PlayerPrefsIndex,
            /// <summary>
            /// Write the option index into the game's own settings store through its static
            /// <c>SetInt(string, int)</c> (and save/re-apply methods), found by name at runtime, e.g. Hellasure's
            /// <c>Game.UI.SettingsSaveManager</c>. No compile-time dependency on the game's code.
            /// </summary>
            GameSettingsStore,
        }

        [Tooltip("This game's id in the launcher. Leave empty to derive it from Project Settings > Product Name (\"Rick's Lewd Universe\" -> ricks-lewd-universe).")]
        public string gameId = "";

        [Tooltip("Where the launcher's language goes. It is applied once per launcher change, so a language picked in the game is kept.")]
        public LanguageTarget languageTarget = LanguageTarget.None;

        [Tooltip("PlayerPrefsIndex: the PlayerPrefs int key the game's language menu reads.")]
        public string playerPrefsKey = "currentOption";

        [Tooltip("PlayerPrefsIndex / GameSettingsStore: language codes in option order (index 0 = first option), matched by primary language (\"pt\" also covers \"pt-BR\"). Leave empty to use Unity Localization's locale order (needs com.unity.localization).")]
        public List<string> languageCodes = new List<string>();

        [Header("GameSettingsStore")]
        [Tooltip("Full name of the static class that stores the game's settings, e.g. Game.UI.SettingsSaveManager.")]
        public string settingsStoreType = "";

        [Tooltip("Static method taking (string key, int value).")]
        public string settingsStoreSetIntMethod = "SetInt";

        [Tooltip("Optional static method without parameters that writes the store to disk, e.g. SaveSettings.")]
        public string settingsStoreSaveMethod = "SaveSettings";

        [Tooltip("Keys that receive the language index, e.g. OptionPicker_Language and OptionPicker_DialogueLanguage.")]
        public List<string> settingsStoreKeys = new List<string>();

        [Tooltip("Optional \"Namespace.Type.Method\" (static, no parameters) the game uses to apply its saved settings, called after writing, e.g. Game.UI.SettingsBootstrap.ReloadAndApply.")]
        public string reapplyMethod = "";

        [Tooltip("Optional: a PlayerPrefs string key where an older per-game script kept its \"last applied\" marker. Carried over once so players keep their in-game choice.")]
        public string legacyMarkerPrefKey = "";

        [Tooltip("Re-read the file while the game runs and raise ViznitySharedSettings.Changed (checks a timestamp every 2 s).")]
        public bool watchForChanges;

        /// <summary>The menu index for a language code in <see cref="languageCodes"/>, or -1.</summary>
        public int IndexOf(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode) || languageCodes == null) return -1;
            for (int i = 0; i < languageCodes.Count; i++)
            {
                if (string.Equals(languageCodes[i], languageCode, System.StringComparison.OrdinalIgnoreCase)) return i;
            }
            string primary = LanguageSync.PrimarySubtag(languageCode);
            for (int i = 0; i < languageCodes.Count; i++)
            {
                if (LanguageSync.PrimarySubtag(languageCodes[i]) == primary) return i;
            }
            return -1;
        }

        /// <summary>True when the language index is taken from Unity Localization's locales instead of <see cref="languageCodes"/>.</summary>
        public bool UsesLocaleOrder =>
            (languageTarget == LanguageTarget.PlayerPrefsIndex || languageTarget == LanguageTarget.GameSettingsStore) &&
            (languageCodes == null || languageCodes.Count == 0);

        /// <summary>The config from Resources, or null when the game has none.</summary>
        public static ViznitySharedSettingsConfig Load() => Resources.Load<ViznitySharedSettingsConfig>(ResourceName);
    }
}
