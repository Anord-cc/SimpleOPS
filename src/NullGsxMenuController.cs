// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
using System.Collections.Generic;

namespace SimpleOps.GsxRamp
{
    internal sealed class NullGsxMenuController : IGsxMenuController
    {
        private readonly string _reason;

        public NullGsxMenuController(string reason)
        {
            _reason = reason;
            StatusText = string.IsNullOrWhiteSpace(reason) ? "GSX unavailable." : reason;
        }

        public string StatusText { get; private set; }

        public string GetTooltip()
        {
            return null;
        }

        public IList<string> GetMenuLines()
        {
            return new string[0];
        }

        public MenuSelectionResult OpenAndSelect(string reason, params string[] patterns)
        {
            return MenuSelectionResult.NotDetected(_reason ?? "GSX is unavailable.");
        }

        public MenuSelectionResult TrySelectExisting(params string[] patterns)
        {
            return MenuSelectionResult.NotDetected(_reason ?? "GSX is unavailable.");
        }
    }
}
