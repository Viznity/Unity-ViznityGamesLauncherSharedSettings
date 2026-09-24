namespace Viznity.SharedSettings
{
    /// <summary>
    /// JSON keys Viznity Games Desktop writes into <c>shared-settings.json</c>
    /// (launcher: <c>src-tauri/src/shared_settings.rs</c>). Keep the two lists in sync when a key is added.
    /// </summary>
    public static class SharedSettingKeys
    {
        /// <summary>Number. Highest schema the file has been written with. Readers never refuse a newer one.</summary>
        public const string SchemaVersion = "schema_version";

        /// <summary>Bool, default true. Show achievement unlock toasts in games.</summary>
        public const string AchievementNotificationsEnabled = "achievement_notifications_enabled";

        /// <summary>Bool, default false. Games should use the launcher's language (or the per-game one).</summary>
        public const string GameLanguageSyncEnabled = "game_language_sync_enabled";

        /// <summary>String. The launcher's UI language as a BCP 47-style code ("en", "tr", "pt-BR").</summary>
        public const string LauncherLanguage = "launcher_language";

        /// <summary>Object <c>{ "&lt;game id&gt;": "&lt;language code&gt;" }</c>. A language picked for one game; wins over <see cref="LauncherLanguage"/>.</summary>
        public const string GameLanguages = "game_languages";

        /// <summary>Bool, default false. Skip the studio intro. The launcher also passes <c>-nointro</c> when it starts a game.</summary>
        public const string SkipIntro = "skip_intro";
    }
}
