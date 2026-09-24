using UnityEngine;

namespace Viznity.SharedSettings
{
    /// <summary>
    /// Hidden, DontDestroyOnLoad poller behind <see cref="ViznitySharedSettings.WatchForChanges"/>. Checks the file's
    /// timestamp (not its content) every few seconds on the main thread, so <see cref="ViznitySharedSettings.Changed"/>
    /// handlers can touch Unity objects directly. Pauses while the application is not focused and re-checks on focus.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class SharedSettingsWatcher : MonoBehaviour
    {
        /// <summary>Seconds between timestamp checks.</summary>
        public static float IntervalSeconds = 2f;

        private static SharedSettingsWatcher _instance;
        private float _next;

        public static bool IsRunning => _instance != null;

        internal static void Run()
        {
            if (_instance != null || !Application.isPlaying) return;
            var go = new GameObject("[Viznity Shared Settings]") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SharedSettingsWatcher>();
        }

        internal static void Halt()
        {
            if (_instance == null) return;
            Destroy(_instance.gameObject);
            _instance = null;
        }

        private void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + Mathf.Max(0.25f, IntervalSeconds);
            ViznitySharedSettings.ReloadIfFileChanged();
        }

        private void OnApplicationFocus(bool focused)
        {
            // Coming back from the launcher is exactly when a setting may have changed.
            if (focused) ViznitySharedSettings.ReloadIfFileChanged();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
