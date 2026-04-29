using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SimpleOps.GsxRamp
{
    internal sealed class GsxMenuDriver : IGsxMenuController
    {
        private readonly GsxPaths _paths;
        private readonly GsxHotkeySender _hotkeySender;

        public GsxMenuDriver(GsxPaths paths, GsxHotkeySender hotkeySender)
        {
            _paths = paths;
            _hotkeySender = hotkeySender;
        }

        public string GetTooltip()
        {
            if (!File.Exists(_paths.GsxTooltipPath))
            {
                return null;
            }

            try
            {
                return string.Join(" ", File.ReadAllLines(_paths.GsxTooltipPath)).Trim();
            }
            catch
            {
                return null;
            }
        }

        public IList<string> GetMenuLines()
        {
            if (!File.Exists(_paths.GsxMenuPath))
            {
                return new List<string>();
            }

            try
            {
                return File.ReadAllLines(_paths.GsxMenuPath)
                    .Select(line => (line ?? string.Empty).Trim())
                    .Where(line => line.Length > 0)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public MenuSelectionResult OpenAndSelect(string reason, params string[] patterns)
        {
            var openedAtUtc = DateTime.UtcNow;
            _hotkeySender.OpenMenu();
            var menuLines = WaitForMenuUpdate(openedAtUtc, 8000);
            if (menuLines == null)
            {
                return MenuSelectionResult.NotDetected("GSX menu was not detected for " + reason + ".");
            }

            return SelectAndSend(menuLines, patterns);
        }

        public MenuSelectionResult TrySelectExisting(params string[] patterns)
        {
            return SelectAndSend(GetMenuLines(), patterns);
        }

        public IList<string> WaitForMenuUpdate(DateTime afterUtc, int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                if (File.Exists(_paths.GsxMenuPath))
                {
                    var info = new FileInfo(_paths.GsxMenuPath);
                    var lines = GetMenuLines();
                    if (info.LastWriteTimeUtc > afterUtc && lines.Count >= 2)
                    {
                        return lines;
                    }
                }

                Thread.Sleep(150);
            }

            return null;
        }

        public static MenuSelectionResult SelectFromMenu(IList<string> menuLines, params string[] patterns)
        {
            var match = MatchMenuChoice(menuLines, patterns);
            if (match.Status == MenuMatchStatus.Selected)
            {
                return MenuSelectionResult.Selected(match.Index, match.Text, match.Reason);
            }

            if (match.Status == MenuMatchStatus.Ambiguous)
            {
                return MenuSelectionResult.Ambiguous(match.Reason);
            }

            if (match.Status == MenuMatchStatus.NotDetected)
            {
                return MenuSelectionResult.NotDetected(match.Reason);
            }

            return MenuSelectionResult.NotFound(match.Reason);
        }

        private MenuSelectionResult SelectAndSend(IList<string> menuLines, params string[] patterns)
        {
            var selection = SelectFromMenu(menuLines, patterns);
            if (selection.WasSelected)
            {
                _hotkeySender.SelectChoice(selection.Index);
                Thread.Sleep(250);
            }

            return selection;
        }

        public static MenuMatchResult MatchMenuChoice(IList<string> menuLines, params string[] patterns)
        {
            if (menuLines == null || menuLines.Count < 2)
            {
                return MenuMatchResult.NotDetected("GSX menu is empty or unavailable.");
            }

            var requestVariants = BuildRequestVariants(patterns);
            var options = new List<MenuOption>();
            for (int i = 1; i < menuLines.Count; i++)
            {
                options.Add(new MenuOption(i - 1, menuLines[i]));
            }

            var exactMatches = options.Where(option => requestVariants.Contains(option.Normalized) || requestVariants.Contains(option.Canonical)).ToList();
            if (exactMatches.Count == 1)
            {
                return MenuMatchResult.Selected(exactMatches[0], "Exact normalized menu match.");
            }

            if (exactMatches.Count > 1)
            {
                return MenuMatchResult.Ambiguous("Multiple exact GSX menu matches: " + string.Join(" | ", exactMatches.Select(x => x.Text).ToArray()));
            }

            var containsMatches = options.Where(option => requestVariants.Any(request => option.Normalized.Contains(request) || option.Canonical.Contains(request))).ToList();
            if (containsMatches.Count == 1)
            {
                return MenuMatchResult.Selected(containsMatches[0], "Contains-match menu match.");
            }

            if (containsMatches.Count > 1)
            {
                return MenuMatchResult.Ambiguous("Multiple contains GSX menu matches: " + string.Join(" | ", containsMatches.Select(x => x.Text).ToArray()));
            }

            var tokenMatches = options.Where(option => requestVariants.Any(request => TokenSubsetMatch(option.Canonical, request))).ToList();
            if (tokenMatches.Count == 1)
            {
                return MenuMatchResult.Selected(tokenMatches[0], "Synonym/token menu match.");
            }

            if (tokenMatches.Count > 1)
            {
                return MenuMatchResult.Ambiguous("Multiple synonym GSX menu matches: " + string.Join(" | ", tokenMatches.Select(x => x.Text).ToArray()));
            }

            return MenuMatchResult.NotFound("No GSX menu match found. Available options: " + DescribeOptions(menuLines));
        }

        public static string DescribeOptions(IList<string> menuLines)
        {
            if (menuLines == null || menuLines.Count < 2)
            {
                return "<no options>";
            }

            return string.Join(" | ", menuLines.Skip(1).ToArray());
        }

        private static HashSet<string> BuildRequestVariants(string[] patterns)
        {
            var variants = new HashSet<string>(StringComparer.Ordinal);
            if (patterns == null)
            {
                return variants;
            }

            for (int i = 0; i < patterns.Length; i++)
            {
                var normalized = TextUtility.NormalizeText(patterns[i]);
                if (normalized.Length == 0)
                {
                    continue;
                }

                variants.Add(normalized);
                variants.Add(TextUtility.CanonicalizeMenuText(normalized));
            }

            return variants;
        }

        private static bool TokenSubsetMatch(string option, string request)
        {
            var requestTokens = request.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < requestTokens.Length; i++)
            {
                if (!option.Contains(requestTokens[i]))
                {
                    return false;
                }
            }

            return requestTokens.Length > 0;
        }
    }

    internal enum MenuMatchStatus
    {
        NotDetected = 0,
        NotFound = 1,
        Ambiguous = 2,
        Selected = 3
    }

    internal sealed class MenuMatchResult
    {
        public MenuMatchStatus Status;
        public int Index;
        public string Text;
        public string Reason;

        public static MenuMatchResult Selected(MenuOption option, string reason)
        {
            return new MenuMatchResult { Status = MenuMatchStatus.Selected, Index = option.Index, Text = option.Text, Reason = reason };
        }

        public static MenuMatchResult Ambiguous(string reason)
        {
            return new MenuMatchResult { Status = MenuMatchStatus.Ambiguous, Reason = reason };
        }

        public static MenuMatchResult NotFound(string reason)
        {
            return new MenuMatchResult { Status = MenuMatchStatus.NotFound, Reason = reason };
        }

        public static MenuMatchResult NotDetected(string reason)
        {
            return new MenuMatchResult { Status = MenuMatchStatus.NotDetected, Reason = reason };
        }
    }

    internal sealed class MenuSelectionResult
    {
        public bool WasSelected;
        public int Index;
        public string Text;
        public string Reason;

        public static MenuSelectionResult Selected(int index, string text, string reason)
        {
            return new MenuSelectionResult { WasSelected = true, Index = index, Text = text, Reason = reason };
        }

        public static MenuSelectionResult Ambiguous(string reason)
        {
            return new MenuSelectionResult { WasSelected = false, Reason = reason };
        }

        public static MenuSelectionResult NotFound(string reason)
        {
            return new MenuSelectionResult { WasSelected = false, Reason = reason };
        }

        public static MenuSelectionResult NotDetected(string reason)
        {
            return new MenuSelectionResult { WasSelected = false, Reason = reason };
        }
    }

    internal sealed class MenuOption
    {
        public readonly int Index;
        public readonly string Text;
        public readonly string Normalized;
        public readonly string Canonical;

        public MenuOption(int index, string text)
        {
            Index = index;
            Text = text;
            Normalized = TextUtility.NormalizeText(text);
            Canonical = TextUtility.CanonicalizeMenuText(text);
        }
    }
}
