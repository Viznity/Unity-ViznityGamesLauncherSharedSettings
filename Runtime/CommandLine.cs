using System;

namespace Viznity.SharedSettings
{
    /// <summary>Launch arguments, e.g. the <c>-nointro</c> the launcher adds.</summary>
    public static class CommandLine
    {
        private static string[] _args;

        /// <summary>For tests; null restores the real command line.</summary>
        internal static string[] ArgsOverride { set => _args = value; }

        private static string[] Args => _args ?? (_args = SafeArgs());

        /// <summary>True when <paramref name="flag"/> was passed (case-insensitive, "-flag" and "--flag" both count).</summary>
        public static bool HasFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag)) return false;
            string bare = flag.TrimStart('-');
            foreach (string arg in Args)
            {
                if (arg != null && arg.StartsWith("-", StringComparison.Ordinal) &&
                    string.Equals(arg.TrimStart('-'), bare, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static string[] SafeArgs()
        {
            try { return Environment.GetCommandLineArgs(); }
            catch (NotSupportedException) { return Array.Empty<string>(); } // WebGL
        }
    }
}
