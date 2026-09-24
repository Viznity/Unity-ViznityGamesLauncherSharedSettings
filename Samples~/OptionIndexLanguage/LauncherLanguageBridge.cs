using UnityEngine;
using Viznity.SharedSettings;

// Sample: a game whose language menu stores an option index in PlayerPrefs and applies it in its own
// Start (Rick's Lewd Universe's ChangeLanguage.cs). Copy into the game, then set GameId and the table.
public static class LauncherLanguageBridge
{
    /// <summary>This game's id in the launcher (Viznity Games Desktop's game list / game_languages keys).</summary>
    private const string GameId = "ricks-lewd-universe";

    /// <summary>The PlayerPrefs key the game's language menu reads.</summary>
    private const string LanguageOptionPref = "currentOption";

    // Before the first scene, so the language menu's Start already sees the new option.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        ViznitySharedSettings.GameId = GameId;
        if (!LanguageSync.TryTakeChangedLanguage(out string code)) return;

        int option = OptionFor(code);
        if (option < 0)
        {
            Debug.Log($"[LauncherLanguageBridge] This game does not ship '{code}'; keeping its own language.");
            return;
        }
        PlayerPrefs.SetInt(LanguageOptionPref, option);
        PlayerPrefs.Save();
        Debug.Log($"[LauncherLanguageBridge] Using the launcher language '{code}'.");
    }

    /// <summary>The menu's option index for a language code, or -1 when the game does not ship it.</summary>
    private static int OptionFor(string code)
    {
        switch (LanguageSync.PrimarySubtag(code))
        {
            case "en": return 0;
            case "ru": return 1;
            case "tr": return 2;
            case "fr": return 3;
            case "it": return 4;
            case "de": return 5;
            case "es": return 6;
            case "pt": return 7;
            case "ja": return 8;
            case "ko": return 9;
            case "zh": return 10;
            default: return -1;
        }
    }
}
