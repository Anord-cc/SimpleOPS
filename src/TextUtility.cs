using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleOps.GsxRamp
{
    internal static class TextUtility
    {
        private static readonly Dictionary<string, string> CanonicalPhraseMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "flight deck", "cockpit" },
            { "ground crew", "ramp" },
            { "jet bridge", "jetway" },
            { "jetbridge", "jetway" },
            { "bags", "baggage" },
            { "bag", "baggage" },
            { "refuel", "refueling" },
            { "refueling", "fueling" },
            { "fuel truck", "refueling" },
            { "de ice", "deicing" },
            { "deice", "deicing" },
            { "de planing", "deboarding" },
            { "deplaning", "deboarding" },
            { "disembarkation", "deboarding" },
            { "push back", "pushback" },
            { "passenger stairs", "stairs" },
            { "catering truck", "catering" },
            { "head set", "headset" },
            { "follow me car", "follow me" },
            { "tow bar", "towbar" },
            { "jet way", "jetway" }
        };

        public static string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var chars = text.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ').ToArray();
            var normalized = string.Join(" ", new string(chars).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            return ApplyCanonicalPhraseMap(normalized);
        }

        public static string CanonicalizeMenuText(string text)
        {
            var normalized = NormalizeText(text);
            normalized = ReplaceWholeWord(normalized, "jet bridge", "jetway");
            normalized = ReplaceWholeWord(normalized, "baggage", "baggage");
            normalized = ReplaceWholeWord(normalized, "fuel", "refueling");
            normalized = ReplaceWholeWord(normalized, "deice", "deicing");
            normalized = ReplaceWholeWord(normalized, "push", "pushback");
            normalized = ReplaceWholeWord(normalized, "stairs", "passenger stairs");
            normalized = ReplaceWholeWord(normalized, "catering truck", "catering");
            normalized = ReplaceWholeWord(normalized, "jetway", "jetway");
            normalized = ReplaceWholeWord(normalized, "jetbridge", "jetway");
            return string.Join(" ", normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public static bool ContainsAny(string normalized, params string[] phrases)
        {
            for (int i = 0; i < phrases.Length; i++)
            {
                var candidate = NormalizeText(phrases[i]);
                if (candidate.Length > 0 && normalized.Contains(candidate))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ContainsAll(string normalized, params string[] phrases)
        {
            for (int i = 0; i < phrases.Length; i++)
            {
                var candidate = NormalizeText(phrases[i]);
                if (candidate.Length == 0 || !normalized.Contains(candidate))
                {
                    return false;
                }
            }

            return true;
        }

        public static string[] Tokenize(string text)
        {
            return NormalizeText(text).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string ApplyCanonicalPhraseMap(string normalized)
        {
            foreach (var pair in CanonicalPhraseMap)
            {
                normalized = ReplaceWholeWord(normalized, pair.Key, pair.Value);
            }

            return string.Join(" ", normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private static string ReplaceWholeWord(string input, string source, string replacement)
        {
            var padded = " " + input + " ";
            var needle = " " + source + " ";
            while (padded.IndexOf(needle, StringComparison.Ordinal) >= 0)
            {
                padded = padded.Replace(needle, " " + replacement + " ");
            }

            return padded.Trim();
        }
    }
}
