// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
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
            string error;
            var paths = TryDetect(out error);
            if (paths == null)
            {
                throw new InvalidOperationException(error);
            }

            return paths;
        }

        public static GsxPaths TryDetect(out string error)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Fsdreamteam"))
            {
                if (key == null)
                {
                    error = @"FSDreamTeam registry key not found at HKCU\Software\Fsdreamteam.";
                    return null;
                }

                var root = key.GetValue("root") as string;
                if (string.IsNullOrWhiteSpace(root))
                {
                    error = @"FSDreamTeam registry value 'root' is missing.";
                    return null;
                }

                var paths = new GsxPaths();
                paths.FsdtRoot = root;
                paths.GsxPanelPath = Path.Combine(root, "MSFS", "fsdreamteam-gsx-pro", "html_ui", "InGamePanels", "FSDT_GSX_Panel");
                paths.GsxMenuPath = Path.Combine(paths.GsxPanelPath, "menu");
                paths.GsxTooltipPath = Path.Combine(paths.GsxPanelPath, "tooltip");
                paths.GsxHotkeyPath = Path.Combine(paths.GsxPanelPath, "hotkey.json");

                if (!Directory.Exists(paths.GsxPanelPath))
                {
                    error = "GSX panel path not found: " + paths.GsxPanelPath;
                    return null;
                }

                if (!File.Exists(paths.GsxHotkeyPath))
                {
                    error = "GSX hotkey.json not found: " + paths.GsxHotkeyPath;
                    return null;
                }

                error = null;
                return paths;
            }
        }
    }
}
