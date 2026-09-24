using System;
using System.IO;

namespace Viznity.SharedSettings
{
    /// <summary>
    /// Where the launcher keeps <c>shared-settings.json</c>, the same folder Rust's <c>dirs::config_dir()</c> returns:
    /// <list type="bullet">
    /// <item>Windows: <c>%APPDATA%\Viznity Games\shared-settings.json</c></item>
    /// <item>macOS: <c>~/Library/Application Support/Viznity Games/shared-settings.json</c></item>
    /// <item>Linux: <c>$XDG_CONFIG_HOME/Viznity Games/shared-settings.json</c>, else <c>~/.config/...</c></item>
    /// </list>
    /// </summary>
    public static class SharedSettingsPath
    {
        public const string VendorFolder = "Viznity Games";
        public const string FileName = "shared-settings.json";

        public enum Os { Windows, MacOS, Linux }

        /// <summary>The file for the platform this build runs on, or null when there is no per-user folder (WebGL, consoles, mobile).</summary>
        public static string Resolve()
        {
            // Editor first: an editor on macOS with the Windows build target also defines UNITY_STANDALONE_WIN.
#if UNITY_EDITOR_WIN
            return Resolve(Os.Windows);
#elif UNITY_EDITOR_OSX
            return Resolve(Os.MacOS);
#elif UNITY_EDITOR_LINUX
            return Resolve(Os.Linux);
#elif UNITY_STANDALONE_WIN
            return Resolve(Os.Windows);
#elif UNITY_STANDALONE_OSX
            return Resolve(Os.MacOS);
#elif UNITY_STANDALONE_LINUX
            return Resolve(Os.Linux);
#elif UNITY_5_3_OR_NEWER
            return null;
#else
            // Outside Unity (tests): the machine running them.
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) return Resolve(Os.Windows);
            return Resolve(Directory.Exists("/System/Library") ? Os.MacOS : Os.Linux);
#endif
        }

        /// <summary>The file for <paramref name="os"/>, using this process's environment.</summary>
        public static string Resolve(Os os)
        {
            return Resolve(os, Environment.GetEnvironmentVariable, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        }

        /// <summary>Testable core. <paramref name="appData"/> is Windows' roaming AppData folder.</summary>
        internal static string Resolve(Os os, Func<string, string> env, string appData)
        {
            string home = env("HOME");
            string configDir;
            switch (os)
            {
                case Os.Windows:
                    configDir = !string.IsNullOrEmpty(appData) ? appData : env("APPDATA");
                    break;
                case Os.MacOS:
                    // Not SpecialFolder.ApplicationData: Mono maps that to ~/.config on macOS.
                    configDir = string.IsNullOrEmpty(home) ? null : Path.Combine(home, "Library", "Application Support");
                    break;
                default:
                    string xdg = env("XDG_CONFIG_HOME");
                    // The XDG spec ignores relative values, and so does the launcher.
                    configDir = !string.IsNullOrEmpty(xdg) && Path.IsPathRooted(xdg)
                        ? xdg
                        : string.IsNullOrEmpty(home) ? null : Path.Combine(home, ".config");
                    break;
            }
            return string.IsNullOrEmpty(configDir) ? null : Path.Combine(configDir, VendorFolder, FileName);
        }
    }
}
