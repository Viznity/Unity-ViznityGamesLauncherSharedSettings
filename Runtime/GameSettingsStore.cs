using System;
using System.Reflection;
using UnityEngine;

namespace Viznity.SharedSettings
{
    /// <summary>
    /// Writes into a game's own settings store by name, so the package never needs a compile-time reference to
    /// game code (which lives in Assembly-CSharp and cannot be referenced from a package). Hellasure:
    /// <c>Game.UI.SettingsSaveManager.SetInt(key, index)</c>, <c>SaveSettings()</c>, then
    /// <c>Game.UI.SettingsBootstrap.ReloadAndApply()</c> so the game applies the language through its own code
    /// (Unity Localization and Dialogue System), and its settings menu shows the same choice.
    /// </summary>
    public static class GameSettingsStore
    {
        private const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        public static bool Write(ViznitySharedSettingsConfig config, int index)
        {
            Type store = FindType(config.settingsStoreType);
            if (store == null) return Fail($"settings store type '{config.settingsStoreType}' was not found");
            MethodInfo setInt = store.GetMethod(config.settingsStoreSetIntMethod, Static, null, new[] { typeof(string), typeof(int) }, null);
            if (setInt == null) return Fail($"{store.FullName}.{config.settingsStoreSetIntMethod}(string, int) was not found");
            if (config.settingsStoreKeys == null || config.settingsStoreKeys.Count == 0) return Fail("no settings store keys are set");

            foreach (string key in config.settingsStoreKeys)
            {
                if (!string.IsNullOrEmpty(key)) setInt.Invoke(null, new object[] { key, index });
            }
            if (!string.IsNullOrEmpty(config.settingsStoreSaveMethod))
            {
                MethodInfo save = store.GetMethod(config.settingsStoreSaveMethod, Static, null, Type.EmptyTypes, null);
                if (save == null) return Fail($"{store.FullName}.{config.settingsStoreSaveMethod}() was not found");
                save.Invoke(null, null);
            }
            if (!string.IsNullOrEmpty(config.reapplyMethod) && !InvokeStatic(config.reapplyMethod))
            {
                return Fail($"re-apply method '{config.reapplyMethod}' was not found");
            }
            return true;
        }

        /// <summary>Calls a static, parameterless "Namespace.Type.Method". False when it does not exist.</summary>
        public static bool InvokeStatic(string qualifiedMethod)
        {
            int dot = qualifiedMethod.LastIndexOf('.');
            if (dot <= 0) return false;
            Type type = FindType(qualifiedMethod.Substring(0, dot));
            MethodInfo method = type?.GetMethod(qualifiedMethod.Substring(dot + 1), Static, null, Type.EmptyTypes, null);
            if (method == null) return false;
            method.Invoke(null, null);
            return true;
        }

        /// <summary>A type by full name in any loaded assembly (game code is usually in Assembly-CSharp).</summary>
        public static Type FindType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null) return type;
            }
            return null;
        }

        private static bool Fail(string reason)
        {
            Debug.LogWarning("[Viznity Shared Settings] Could not write the language: " + reason + ". Check the Game Settings Store fields of the config.");
            return false;
        }
    }
}
