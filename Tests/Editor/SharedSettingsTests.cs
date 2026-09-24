using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Viznity.SharedSettings.Tests
{
    public class SharedSettingsTests
    {
        // What the launcher writes today (shared_settings.rs), plus a key from a "future" launcher.
        private const string LauncherFile = @"{
  ""schema_version"": 1,
  ""achievement_notifications_enabled"": false,
  ""game_language_sync_enabled"": true,
  ""launcher_language"": ""tr"",
  ""game_languages"": { ""hellasure"": ""de"", ""ricks-lewd-universe"": ""pt-BR"", ""bad"": ""../x"" },
  ""skip_intro"": true,
  ""future_setting"": { ""volume"": 0.5, ""tags"": [""a"", ""b""] }
}";

        private sealed class MemoryStore : LanguageSync.IMarkerStore
        {
            public string Marker = string.Empty;
            public string Get() => Marker;
            public void Set(string marker) => Marker = marker;
        }

        [TearDown]
        public void Reset()
        {
            ViznitySharedSettings.ResetForTests();
            CommandLine.ArgsOverride = null;
        }

        [Test]
        public void ReadsEveryKnownSetting()
        {
            var s = SharedSettingsSnapshot.FromJson(LauncherFile);
            Assert.IsTrue(s.Exists);
            Assert.AreEqual(1, s.SchemaVersion);
            Assert.IsFalse(s.AchievementNotificationsEnabled);
            Assert.IsTrue(s.GameLanguageSyncEnabled);
            Assert.AreEqual("tr", s.LauncherLanguage);
            Assert.IsTrue(s.SkipIntro);
            Assert.AreEqual("de", s.GameLanguages["hellasure"]);
            Assert.IsFalse(s.GameLanguages.ContainsKey("bad"), "invalid codes are dropped");
        }

        [Test]
        public void ReadsKeysAddedByNewerLaunchers()
        {
            var s = SharedSettingsSnapshot.FromJson(LauncherFile);
            Assert.IsTrue(s.TryGetObject("future_setting", out var future));
            Assert.AreEqual(0.5, (double)future["volume"]);
            Assert.AreEqual(2, ((List<object>)future["tags"]).Count);
            Assert.IsFalse(s.TryGetBool("future_setting", out _), "wrong type is not coerced");
            Assert.AreEqual(7, s.GetInt("missing", 7));
        }

        [Test]
        public void MissingOrBrokenFileMeansDefaults()
        {
            foreach (var text in new[] { "", "   ", "{\"achievement_notifications_ena", "[1,2]", "null", "{\"a\":1} trailing" })
            {
                var s = SharedSettingsSnapshot.FromJson(text);
                Assert.IsFalse(s.Exists, text);
                Assert.IsTrue(s.AchievementNotificationsEnabled, text);
                Assert.IsFalse(s.GameLanguageSyncEnabled, text);
                Assert.IsNull(s.LanguageFor("hellasure"), text);
            }
        }

        [Test]
        public void ParserHandlesEscapesBomAndDepth()
        {
            var s = SharedSettingsSnapshot.FromJson("\uFEFF{\"k\":\"a\\\"b\\u00e7\\n\",\"n\":-1.5e2,\"t\":true,\"z\":null}");
            Assert.AreEqual("a\"b\u00e7\n", s.GetString("k", null));
            Assert.AreEqual(-150, s.GetNumber("n", 0));
            Assert.IsTrue(s.Has("z"));
            string deep = new string('[', 100) + new string(']', 100);
            Assert.Throws<FormatException>(() => SharedSettingsJson.Parse(deep));
        }

        [Test]
        public void PerGameLanguageWinsAndSyncOffMeansNull()
        {
            var s = SharedSettingsSnapshot.FromJson(LauncherFile);
            Assert.AreEqual("pt-BR", s.LanguageFor("ricks-lewd-universe"));
            Assert.AreEqual("tr", s.LanguageFor("train-with-elsa"));
            Assert.AreEqual("tr", s.LanguageFor(null));
            var off = SharedSettingsSnapshot.FromJson("{\"game_language_sync_enabled\":false,\"launcher_language\":\"tr\"}");
            Assert.IsNull(off.LanguageFor("hellasure"));
        }

        [Test]
        public void LanguageIsHandedOutOncePerChange()
        {
            var store = new MemoryStore();
            var tr = SharedSettingsSnapshot.FromJson("{\"game_language_sync_enabled\":true,\"launcher_language\":\"tr\"}");
            Assert.IsTrue(LanguageSync.TryTakeChangedLanguage(tr, "g", store, out string code));
            Assert.AreEqual("tr", code);
            Assert.IsFalse(LanguageSync.TryTakeChangedLanguage(tr, "g", store, out _), "the player's own choice is kept");

            var off = SharedSettingsSnapshot.FromJson("{\"game_language_sync_enabled\":false,\"launcher_language\":\"tr\"}");
            Assert.IsFalse(LanguageSync.TryTakeChangedLanguage(off, "g", store, out _));
            Assert.IsTrue(LanguageSync.TryTakeChangedLanguage(tr, "g", store, out code), "turning sync back on applies again");

            var perGame = SharedSettingsSnapshot.FromJson("{\"game_language_sync_enabled\":true,\"launcher_language\":\"tr\",\"game_languages\":{\"g\":\"ja\"}}");
            Assert.IsTrue(LanguageSync.TryTakeChangedLanguage(perGame, "g", store, out code));
            Assert.AreEqual("ja", code);

            Assert.IsFalse(LanguageSync.TryTakeChangedLanguage(SharedSettingsSnapshot.Empty, "g", store, out _));
            Assert.AreEqual("on:ja", store.Marker, "no launcher file leaves the marker alone");
        }

        [Test]
        public void ConfigMapsCodesToMenuIndexes()
        {
            var config = UnityEngine.ScriptableObject.CreateInstance<ViznitySharedSettingsConfig>();
            config.languageCodes = new List<string> { "en", "ru", "tr", "pt", "zh-Hans" };
            Assert.AreEqual(2, config.IndexOf("tr"));
            Assert.AreEqual(3, config.IndexOf("pt-BR"), "primary language match");
            Assert.AreEqual(4, config.IndexOf("zh"));
            Assert.AreEqual(-1, config.IndexOf("ja"));
            Assert.AreEqual(-1, config.IndexOf(null));
        }

        [Test]
        public void GameSettingsStoreWritesSavesAndReapplies()
        {
            FakeSettingsStore.Reset();
            var config = UnityEngine.ScriptableObject.CreateInstance<ViznitySharedSettingsConfig>();
            config.languageTarget = ViznitySharedSettingsConfig.LanguageTarget.GameSettingsStore;
            config.settingsStoreType = typeof(FakeSettingsStore).FullName;
            config.settingsStoreKeys = new List<string> { "OptionPicker_Language", "OptionPicker_DialogueLanguage" };
            config.reapplyMethod = typeof(FakeSettingsStore).FullName + ".ReloadAndApply";

            Assert.IsTrue(SharedSettingsBootstrap.WriteLanguageIndex(config, "tr", 3));
            Assert.AreEqual(3, FakeSettingsStore.Values["OptionPicker_Language"]);
            Assert.AreEqual(3, FakeSettingsStore.Values["OptionPicker_DialogueLanguage"]);
            Assert.AreEqual(1, FakeSettingsStore.Saves);
            Assert.AreEqual(1, FakeSettingsStore.Reapplies);

            config.settingsStoreType = "No.Such.Type";
            Assert.IsFalse(SharedSettingsBootstrap.WriteLanguageIndex(config, "tr", 3), "a missing store is reported, not thrown");
        }

        [Test]
        public void GameIdComesFromTheProductName()
        {
            Assert.AreEqual("ricks-lewd-universe", GameIdentity.Slug("Rick's Lewd Universe"));
            Assert.AreEqual("ricks-lewd-universe", GameIdentity.Slug("Rick’s  Lewd   Universe!"));
            Assert.AreEqual("kiva-sucks-at-videogames", GameIdentity.Slug("Kiva Sucks at Videogames"));
            Assert.AreEqual("hellasure", GameIdentity.Slug("Hellasure"));
            Assert.AreEqual("train-with-elsa", GameIdentity.Slug("Train With Elsa"));
            Assert.AreEqual("RicksLewdUniverse", GameIdentity.Pascal("Rick's Lewd Universe"));
            Assert.AreEqual("KivaSucksAtVideogames", GameIdentity.Pascal("Kiva Sucks at Videogames"));
        }

        [Test]
        public void PrimarySubtag()
        {
            Assert.AreEqual("pt", LanguageSync.PrimarySubtag("pt-BR"));
            Assert.AreEqual("zh", LanguageSync.PrimarySubtag("ZH_Hans"));
            Assert.IsNull(LanguageSync.PrimarySubtag(""));
        }

        [Test]
        public void PathsMatchTheLauncher()
        {
            Func<string, string> env = name => name == "HOME" ? "/home/p" : name == "XDG_CONFIG_HOME" ? null : null;
            Assert.AreEqual(Path.Combine("/home/p", ".config", "Viznity Games", "shared-settings.json"),
                SharedSettingsPath.Resolve(SharedSettingsPath.Os.Linux, env, null));
            Func<string, string> xdg = name => name == "HOME" ? "/home/p" : name == "XDG_CONFIG_HOME" ? "/cfg" : null;
            Assert.AreEqual(Path.Combine("/cfg", "Viznity Games", "shared-settings.json"),
                SharedSettingsPath.Resolve(SharedSettingsPath.Os.Linux, xdg, null));
            Func<string, string> relativeXdg = name => name == "HOME" ? "/home/p" : name == "XDG_CONFIG_HOME" ? "cfg" : null;
            StringAssert.StartsWith(Path.Combine("/home/p", ".config"), SharedSettingsPath.Resolve(SharedSettingsPath.Os.Linux, relativeXdg, null));
            Assert.AreEqual(Path.Combine("/Users/p", "Library", "Application Support", "Viznity Games", "shared-settings.json"),
                SharedSettingsPath.Resolve(SharedSettingsPath.Os.MacOS, name => name == "HOME" ? "/Users/p" : null, "/Users/p/.config"));
            Assert.AreEqual(Path.Combine(@"C:\Users\p\AppData\Roaming", "Viznity Games", "shared-settings.json"),
                SharedSettingsPath.Resolve(SharedSettingsPath.Os.Windows, _ => null, @"C:\Users\p\AppData\Roaming"));
        }

        [Test]
        public void ReadsTheFileAndNoticesChanges()
        {
            string dir = Path.Combine(Path.GetTempPath(), "viznity-shared-settings-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, "shared-settings.json");
            try
            {
                ViznitySharedSettings.FilePath = file;
                Assert.IsFalse(ViznitySharedSettings.Current.Exists);

                int changes = 0;
                ViznitySharedSettings.Changed += _ => changes++;
                File.WriteAllText(file, "{\"skip_intro\":true}");
                Assert.IsTrue(ViznitySharedSettings.ReloadIfFileChanged());
                Assert.IsTrue(ViznitySharedSettings.Current.SkipIntro);
                Assert.AreEqual(1, changes);
                Assert.IsFalse(ViznitySharedSettings.ReloadIfFileChanged(), "unchanged file is not read again");

                File.WriteAllText(file, "{\"skip_intro\":false,\"x\":1}");
                ViznitySharedSettings.Reload();
                Assert.IsFalse(ViznitySharedSettings.Current.SkipIntro);
                Assert.AreEqual(2, changes);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public void SkipIntroFromCommandLineOrFile()
        {
            ViznitySharedSettings.FilePath = Path.Combine(Path.GetTempPath(), "does-not-exist-" + Guid.NewGuid().ToString("N") + ".json");
            CommandLine.ArgsOverride = new[] { "Game.exe", "-NoIntro" };
            Assert.IsTrue(ViznitySharedSettings.ShouldSkipIntro);
            CommandLine.ArgsOverride = new[] { "Game.exe", "nointro" };
            Assert.IsFalse(ViznitySharedSettings.ShouldSkipIntro, "a bare word is not a flag");
        }
    }

    // Stands in for Hellasure's Game.UI.SettingsSaveManager / SettingsBootstrap.
    public static class FakeSettingsStore
    {
        public static readonly Dictionary<string, int> Values = new Dictionary<string, int>();
        public static int Saves;
        public static int Reapplies;
        public static void Reset() { Values.Clear(); Saves = 0; Reapplies = 0; }
        public static void SetInt(string key, int value) => Values[key] = value;
        public static void SaveSettings() => Saves++;
        public static void ReloadAndApply() => Reapplies++;
    }
}
