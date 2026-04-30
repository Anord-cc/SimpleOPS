// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
using System;
using System.Collections.Generic;

namespace SimpleOps.GsxRamp
{
    internal sealed class RecognizedPhrase
    {
        public string Text;
        public float Confidence;
        public float AudioLevel;
    }

    internal interface ISpeechInputService : IDisposable
    {
        string StatusText { get; }
        float LastInputLevel { get; }
        void Start(IEnumerable<string> phrases, Action<RecognizedPhrase> onRecognized);
        void Stop();
    }

    internal interface IVoiceOutputService : IDisposable
    {
        string StatusText { get; }
        bool IsEnabled { get; }
        void SpeakAsync(string message);
    }
}
