using System;
using System.IO;
using Microsoft.Win32;

namespace SimpleOps.GsxRamp
{
    internal sealed class GsxPaths
    {
        public string FsdtRoot;
        public string GsxPanelPath;
        public string GsxMenuPath;
        public string GsxTooltipPath;
        public string GsxHotkeyPath;

        public static GsxPaths Detect()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Fsdreamteam"))
            {
                if (key == null)
                {
                    throw new InvalidOperationException(@"FSDreamTeam registry key not found at HKCU\Software\Fsdreamteam.");
                }

                var root = key.GetValue("root") as string;
                if (string.IsNullOrWhiteSpace(root))
                {
                    throw new InvalidOperationException(@"FSDreamTeam registry value 'root' is missing.");
                }

                var paths = new GsxPaths();
                paths.FsdtRoot = root;
                paths.GsxPanelPath = Path.Combine(root, "MSFS", "fsdreamteam-gsx-pro", "html_ui", "InGamePanels", "FSDT_GSX_Panel");
                paths.GsxMenuPath = Path.Combine(paths.GsxPanelPath, "menu");
                paths.GsxTooltipPath = Path.Combine(paths.GsxPanelPath, "tooltip");
                paths.GsxHotkeyPath = Path.Combine(paths.GsxPanelPath, "hotkey.json");

                if (!Directory.Exists(paths.GsxPanelPath))
                {
                    throw new InvalidOperationException("GSX panel path not found: " + paths.GsxPanelPath);
                }

                if (!File.Exists(paths.GsxHotkeyPath))
                {
                    throw new InvalidOperationException("GSX hotkey.json not found: " + paths.GsxHotkeyPath);
                }

                return paths;
            }
        }
    }
}
