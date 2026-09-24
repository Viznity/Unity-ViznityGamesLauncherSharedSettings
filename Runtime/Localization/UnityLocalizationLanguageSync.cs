using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Viznity.SharedSettings.Localization
{
    /// <summary>
    /// Selects the launcher's language in Unity Localization (<c>LocalizationSettings.SelectedLocale</c>).
    /// Compiled only when the project has <c>com.unity.localization</c>; otherwise this assembly is skipped
    /// entirely, so the package still works in games without it.
    /// <code>
    /// ViznitySharedSettings.GameId = "my-game";
    /// UnityLocalizationLanguageSync.ApplyWhenReady();   // e.g. from a RuntimeInitializeOnLoadMethod
    /// </code>
    /// Uses <see cref="LanguageSync"/>'s once-per-change rule: a locale the player later picks in the game is kept
    /// until the launcher's language changes again.
    /// </summary>
    public static class UnityLocalizationLanguageSync
    {
        /// <summary>
        /// Applies the config's UnityLocalization target. Reads the config itself (and sets the game id), since
        /// Unity does not order two BeforeSceneLoad methods in different assemblies.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyFromConfig()
        {
            try
            {
                var config = ViznitySharedSettingsConfig.Load();
                if (config == null) return;
                if (!string.IsNullOrEmpty(config.gameId)) ViznitySharedSettings.GameId = config.gameId;
                if (config.languageTarget == ViznitySharedSettingsConfig.LanguageTarget.UnityLocalization) ApplyWhenReady();
                else if (config.UsesLocaleOrder) ApplyIndexWhenReady(config);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Viznity Shared Settings] Localization sync skipped: " + e.Message);
            }
        }

        /// <summary>
        /// PlayerPrefsIndex / GameSettingsStore without a code list: the option index is the locale's position in
        /// <c>LocalizationSettings.AvailableLocales</c> (Hellasure's settings use exactly that order).
        /// </summary>
        public static void ApplyIndexWhenReady(ViznitySharedSettingsConfig config)
        {
            if (!LanguageSync.TryTakeChangedLanguage(out string code)) return;
            Run(Apply());

            IEnumerator Apply()
            {
                var init = LocalizationSettings.InitializationOperation;
                if (!init.IsDone) yield return init;
                Locale locale = FindLocale(code);
                var locales = LocalizationSettings.AvailableLocales?.Locales;
                int index = locale != null && locales != null ? locales.IndexOf(locale) : -1;
                if (index < 0)
                {
                    Debug.Log($"[Viznity Shared Settings] This game has no '{code}' locale; keeping its own language.");
                    yield break;
                }
                SharedSettingsBootstrap.WriteLanguageIndex(config, code, index);
            }
        }

        /// <summary>
        /// Waits for Localization to initialize, then selects the launcher's locale if it changed. Safe to call
        /// before the first scene; runs on a hidden helper object that removes itself afterwards.
        /// </summary>
        public static void ApplyWhenReady()
        {
            if (!LanguageSync.TryTakeChangedLanguage(out string code)) return;
            Run(SelectLocale(code));
        }

        private static IEnumerator SelectLocale(string code)
        {
            var init = LocalizationSettings.InitializationOperation;
            if (!init.IsDone) yield return init;
            Locale locale = FindLocale(code);
            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
                Debug.Log($"[Viznity Shared Settings] Using the launcher language '{code}' ({locale.Identifier.Code}).");
            }
            else
            {
                Debug.Log($"[Viznity Shared Settings] This game has no '{code}' locale; keeping its own language.");
            }
        }

        /// <summary>Runs a coroutine on a hidden helper object that removes itself when done.</summary>
        private static void Run(IEnumerator routine)
        {
            var go = new GameObject("[Viznity Localization Sync]") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>().Begin(routine);
        }

        /// <summary>
        /// The project's locale for a language code: exact ("pt-BR"), then the same primary language ("pt" for
        /// "pt-BR", "pt-BR" for "pt"), else null. Localization must be initialized.
        /// </summary>
        public static Locale FindLocale(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode)) return null;
            var locales = LocalizationSettings.AvailableLocales?.Locales;
            if (locales == null) return null;
            foreach (var locale in locales)
            {
                if (string.Equals(locale.Identifier.Code, languageCode, System.StringComparison.OrdinalIgnoreCase)) return locale;
            }
            string primary = LanguageSync.PrimarySubtag(languageCode);
            foreach (var locale in locales)
            {
                if (LanguageSync.PrimarySubtag(locale.Identifier.Code) == primary) return locale;
            }
            return null;
        }

        private sealed class Runner : MonoBehaviour
        {
            public void Begin(IEnumerator routine) => StartCoroutine(RunThenRemove(routine));

            private IEnumerator RunThenRemove(IEnumerator routine)
            {
                yield return routine;
                Destroy(gameObject);
            }
        }
    }
}
