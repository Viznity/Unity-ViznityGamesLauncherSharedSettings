using System;
using System.IO;
using UnityEngine;

namespace Viznity.SharedSettings
{
    /// <summary>
    /// Entry point: the settings Viznity Games Desktop shares with installed games.
    /// <code>
    /// ViznitySharedSettings.GameId = "ricks-lewd-universe";          // once, at startup
    /// string language = ViznitySharedSettings.Current.LanguageFor(ViznitySharedSettings.GameId);
    /// if (ViznitySharedSettings.ShouldSkipIntro) SkipIntro();
    /// bool toasts = ViznitySharedSettings.Current.GetBool("some_future_key", true);
    /// </code>
    /// Reading never throws and never blocks on the network; without a launcher every setting has its default.
    /// </summary>
    public static class ViznitySharedSettings
    {
        /// <summary>Files larger than this are ignored (the real file is well under 1 KB).</summary>
        public const long MaxFileBytes = 256 * 1024;

        private static readonly object Gate = new object();
        private static SharedSettingsSnapshot _current;
        private static string _pathOverride;
        private static DateTime _lastWriteUtc;
        private static long _lastLength = -1;

        /// <summary>
        /// This game's id in the launcher (the keys of <c>game_languages</c>, e.g. "ricks-lewd-universe").
        /// Set it once before using the per-game helpers.
        /// </summary>
        public static string GameId { get; set; }

        /// <summary>The file being read. Override for tests or a custom location; null restores the default.</summary>
        public static string FilePath
        {
            get => _pathOverride ?? SharedSettingsPath.Resolve();
            set
            {
                lock (Gate) { _pathOverride = value; _current = null; _lastLength = -1; }
            }
        }

        /// <summary>The latest reading. Loaded on first use; refreshed by <see cref="Reload"/> or the change watcher.</summary>
        public static SharedSettingsSnapshot Current
        {
            get
            {
                lock (Gate)
                {
                    if (_current == null) _current = ReadFile(FilePath, out _lastWriteUtc, out _lastLength);
                    return _current;
                }
            }
        }

        /// <summary>
        /// Raised on the main thread when the file changed and was read again (only while
        /// <see cref="WatchForChanges"/> is on, or after <see cref="Reload"/> found a change).
        /// </summary>
        public static event Action<SharedSettingsSnapshot> Changed;

        /// <summary>Reads the file again now. Raises <see cref="Changed"/> when its content differs.</summary>
        public static SharedSettingsSnapshot Reload()
        {
            SharedSettingsSnapshot previous, next;
            lock (Gate)
            {
                previous = _current;
                next = ReadFile(FilePath, out _lastWriteUtc, out _lastLength);
                _current = next;
            }
            if (previous == null || previous.ToString() != next.ToString()) RaiseChanged(next);
            return next;
        }

        /// <summary>
        /// Checks the file's timestamp and size (cheap, no read) and reloads only when they changed.
        /// The watcher calls this every couple of seconds; call it yourself e.g. when the game regains focus.
        /// </summary>
        public static bool ReloadIfFileChanged()
        {
            string path = FilePath;
            GetStamp(path, out DateTime writeUtc, out long length);
            lock (Gate)
            {
                if (_current != null && writeUtc == _lastWriteUtc && length == _lastLength) return false;
            }
            Reload();
            return true;
        }

        /// <summary>
        /// Watch the file while the game runs, so a setting changed in the launcher applies without a restart.
        /// Off by default: most settings are only read at startup. Polls the timestamp every
        /// <see cref="SharedSettingsWatcher.IntervalSeconds"/> (FileSystemWatcher is unreliable under Mono on macOS).
        /// </summary>
        public static bool WatchForChanges
        {
            get => SharedSettingsWatcher.IsRunning;
            set { if (value) SharedSettingsWatcher.Run(); else SharedSettingsWatcher.Halt(); }
        }

        /// <summary>The game was started with <c>-nointro</c> (the launcher adds it when "Skip game intros" is on).</summary>
        public static bool LaunchedWithNoIntro => CommandLine.HasFlag("-nointro");

        /// <summary>Skip the intro: <c>-nointro</c> on the command line, or the shared setting (for shortcut/Steam starts).</summary>
        public static bool ShouldSkipIntro => LaunchedWithNoIntro || Current.SkipIntro;

        /// <summary>The language the launcher wants for <see cref="GameId"/>, or null (keep the game's own choice).</summary>
        public static string Language => Current.LanguageFor(GameId);

        internal static SharedSettingsSnapshot ReadFile(string path, out DateTime writeUtc, out long length)
        {
            GetStamp(path, out writeUtc, out length);
            if (length < 0 || length > MaxFileBytes) return SharedSettingsSnapshot.Empty;
            try
            {
                // Shared read: the launcher replaces the file with an atomic rename while games may be reading it.
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(stream))
                {
                    return SharedSettingsSnapshot.FromJson(reader.ReadToEnd());
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return SharedSettingsSnapshot.Empty;
            }
        }

        private static void GetStamp(string path, out DateTime writeUtc, out long length)
        {
            writeUtc = default;
            length = -1;
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists) return;
                writeUtc = info.LastWriteTimeUtc;
                length = info.Length;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException || e is ArgumentException)
            {
                // Treated as "no file".
            }
        }

        private static void RaiseChanged(SharedSettingsSnapshot snapshot)
        {
            var handlers = Changed;
            if (handlers == null) return;
            foreach (Action<SharedSettingsSnapshot> handler in handlers.GetInvocationList())
            {
                try { handler(snapshot); }
                catch (Exception e) { Debug.LogException(e); } // one bad listener must not stop the others
            }
        }

        internal static void ResetForTests()
        {
            lock (Gate) { _current = null; _pathOverride = null; _lastLength = -1; }
            Changed = null;
            GameId = null;
        }
    }
}
