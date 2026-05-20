using System.Text.RegularExpressions;

namespace Community.PowerToys.Run.Plugin.QuickMaths
{
    public static class MathDetector
    {
        // Used only for global (non-keyword) mode to avoid querying Wolfram on every
        // arbitrary search. Passes if the query looks even loosely math-related:
        //   - contains a digit, OR
        //   - contains a math operator/symbol, OR
        //   - contains a known math keyword (sin, sqrt, pi, etc.)
        // Wolfram Alpha's own NLP handles everything else — we just need a cheap
        // pre-filter so we don't fire on "open chrome" or "weather today".
        private static readonly Regex MathHintRegex = new Regex(
            @"\d|[\+\-\*\/\^\%\=]|\b(sin|cos|tan|log|ln|sqrt|pi|plus|minus|times|divided|percent|square|cube|root|factorial)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Returns true if <paramref name="query"/> looks math-related enough to send
        /// to Wolfram Alpha in global (non-keyword) mode.
        /// When the user explicitly invokes the plugin via the '=' keyword, skip this
        /// check entirely — all queries are intentional.
        /// </summary>
        public static bool IsMathQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return false;
            return MathHintRegex.IsMatch(query);
        }
    }
}
