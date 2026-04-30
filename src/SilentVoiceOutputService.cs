// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
namespace SimpleOps.GsxRamp
{
    internal sealed class SilentVoiceOutputService : IVoiceOutputService
    {
        public SilentVoiceOutputService(string statusText)
        {
            StatusText = statusText;
        }

        public string StatusText { get; private set; }

        public bool IsEnabled
        {
            get { return false; }
        }

        public void SpeakAsync(string message)
        {
        }

        public void Dispose()
        {
        }
    }
}
