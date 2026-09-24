using System.IO;
using UnityEditor;
using UnityEngine;

namespace Viznity.SharedSettings.Editor
{
    internal static class SharedSettingsConfigMenu
    {
        private const string Folder = "Assets/Resources";
        private const string AssetPath = Folder + "/" + ViznitySharedSettingsConfig.ResourceName + ".asset";

        /// <summary>Creates (or selects) the one config the package reads, in a Resources folder where it is found at runtime.</summary>
        [MenuItem("Tools/Viznity/Shared Settings/Create or Select Config")]
        private static void CreateOrSelect()
        {
            var config = AssetDatabase.LoadAssetAtPath<ViznitySharedSettingsConfig>(AssetPath) ?? ViznitySharedSettingsConfig.Load();
            if (config == null)
            {
                if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Resources");
                config = ScriptableObject.CreateInstance<ViznitySharedSettingsConfig>();
                AssetDatabase.CreateAsset(config, AssetPath);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        [MenuItem("Tools/Viznity/Shared Settings/Show Current File")]
        private static void ShowFile()
        {
            string path = ViznitySharedSettings.FilePath;
            var snapshot = ViznitySharedSettings.Reload();
            Debug.Log($"[Viznity Shared Settings] {path}\n{snapshot}");
            if (!string.IsNullOrEmpty(path) && File.Exists(path)) EditorUtility.RevealInFinder(path);
        }
    }
}
