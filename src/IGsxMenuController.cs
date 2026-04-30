// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
using System.Collections.Generic;

namespace SimpleOps.GsxRamp
{
    internal interface IGsxMenuController
    {
        string StatusText { get; }
        string GetTooltip();
        IList<string> GetMenuLines();
        MenuSelectionResult OpenAndSelect(string reason, params string[] patterns);
        MenuSelectionResult TrySelectExisting(params string[] patterns);
    }
}
