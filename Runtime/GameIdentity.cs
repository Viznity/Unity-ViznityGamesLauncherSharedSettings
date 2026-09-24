using System.Text;
using UnityEngine;

namespace Viznity.SharedSettings
{
    /// <summary>
    /// A game's id in the launcher, derived from Project Settings > Product Name, so a script or config copied
    /// into another game needs no edits: "Rick's Lewd Universe" -> "ricks-lewd-universe", "Hellasure" ->
    /// "hellasure". The launcher's ids (releases::GAME_IDS) are exactly these slugs.
    /// </summary>
    public static class GameIdentity
    {
        /// <summary>This game's id: <see cref="Slug"/> of <c>Application.productName</c>.</summary>
        public static string FromProductName() => Slug(Application.productName);

        /// <summary>This game's name without spaces or punctuation: "Rick's Lewd Universe" -> "RicksLewdUniverse" (the GitLab project is that + "Build").</summary>
        public static string PascalFromProductName() => Pascal(Application.productName);

        /// <summary>Lower case, apostrophes dropped, every other run of non-alphanumerics becomes one '-'.</summary>
        public static string Slug(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            var sb = new StringBuilder(name.Length);
            bool dash = false;
            foreach (char raw in name)
            {
                if (IsApostrophe(raw)) continue;
                char c = char.ToLowerInvariant(raw);
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    if (dash && sb.Length > 0) sb.Append('-');
                    sb.Append(c);
                    dash = false;
                }
                else
                {
                    dash = true;
                }
            }
            return sb.ToString();
        }

        /// <summary>Words capitalized and joined, apostrophes dropped: "Kiva Sucks at Videogames" -> "KivaSucksAtVideogames".</summary>
        public static string Pascal(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            var sb = new StringBuilder(name.Length);
            bool startOfWord = true;
            foreach (char c in name)
            {
                if (IsApostrophe(c)) continue;
                bool alnum = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
                if (!alnum)
                {
                    startOfWord = true;
                    continue;
                }
                sb.Append(startOfWord ? char.ToUpperInvariant(c) : c);
                startOfWord = false;
            }
            return sb.ToString();
        }

        private static bool IsApostrophe(char c) => c == '\'' || c == '’' || c == '`';
    }
}
