using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;

namespace Viznity.SharedSettings.Editor
{
    public class SharedSettingsPreBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            EnsureConfigExistsAndPopulated();
        }

        public static ViznitySharedSettingsConfig EnsureConfigExistsAndPopulated()
        {
            const string Folder = "Assets/Resources";
            const string AssetPath = Folder + "/" + ViznitySharedSettingsConfig.ResourceName + ".asset";

            var config = AssetDatabase.LoadAssetAtPath<ViznitySharedSettingsConfig>(AssetPath);
            bool isNew = false;
            
            if (config == null)
            {
                if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Resources");
                config = ScriptableObject.CreateInstance<ViznitySharedSettingsConfig>();
                AssetDatabase.CreateAsset(config, AssetPath);
                isNew = true;
            }

            bool changed = isNew;

            // Populate Game ID
            if (string.IsNullOrEmpty(config.gameId))
            {
                string productName = PlayerSettings.productName;
                // Remove everything except letters and digits, convert to lowercase
                string safeName = Regex.Replace(productName, @"[^a-zA-Z0-9]", "").ToLowerInvariant();
                config.gameId = safeName;
                changed = true;
            }

            // Enable Watch for Changes
            if (!config.watchForChanges)
            {
                config.watchForChanges = true;
                changed = true;
            }

            // Pull Language Codes from Localization Assets
            if (config.languageCodes == null || config.languageCodes.Count == 0)
            {
                var locales = new System.Collections.Generic.List<string>();
                string[] guids = AssetDatabase.FindAssets("t:UnityEngine.Localization.Locale");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var localeAsset = AssetDatabase.LoadMainAssetAtPath(path);
                    if (localeAsset != null)
                    {
                        var serializedObject = new SerializedObject(localeAsset);
                        var prop = serializedObject.FindProperty("m_Identifier.m_Code");
                        if (prop != null && !string.IsNullOrEmpty(prop.stringValue))
                        {
                            locales.Add(prop.stringValue);
                        }
                    }
                }

                if (locales.Count > 0)
                {
                    config.languageCodes = locales.Distinct().ToList();
                    changed = true;
                }
            }
            
            // Set useful options automatically
            if (config.languageTarget == ViznitySharedSettingsConfig.LanguageTarget.None && config.languageCodes.Count > 0)
            {
                config.languageTarget = ViznitySharedSettingsConfig.LanguageTarget.UnityLocalization;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Viznity Shared Settings] Config auto-populated. GameId: {config.gameId}, Languages: {string.Join(", ", config.languageCodes)}");
            }

            return config;
        }
    }
}
