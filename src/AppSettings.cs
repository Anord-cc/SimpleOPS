// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
using System;

namespace SimpleOps.GsxRamp
{
    internal enum AudioOutputChannel
    {
        Both = 0,
        Left = 1,
        Right = 2
    }

    internal sealed class AppSettings
    {
        public string TelemetryUrl = "http://127.0.0.1:4789/telemetry";
        public double MinConfidence = 0.72d;
        public bool DryRun;
        public bool OpenAiVoiceEnabled;
        public string OpenAiModel = "gpt-4o-mini-tts";
        public string OpenAiVoice = "marin";
        public string MicrophoneDeviceName;
        public string SpeakerDeviceId;
        public string SpeakerDeviceName;
        public double InputSensitivityGate = 0.04d;
        public double OutputVolume = 1.0d;
        public AudioOutputChannel OutputChannel = AudioOutputChannel.Both;
        public double OutputPan;

        public AppSettings Clone()
        {
            return new AppSettings
            {
                TelemetryUrl = TelemetryUrl,
                MinConfidence = MinConfidence,
                DryRun = DryRun,
                OpenAiVoiceEnabled = OpenAiVoiceEnabled,
                OpenAiModel = OpenAiModel,
                OpenAiVoice = OpenAiVoice,
                MicrophoneDeviceName = MicrophoneDeviceName,
                SpeakerDeviceId = SpeakerDeviceId,
                SpeakerDeviceName = SpeakerDeviceName,
                InputSensitivityGate = InputSensitivityGate,
                OutputVolume = OutputVolume,
                OutputChannel = OutputChannel,
                OutputPan = OutputPan
            };
        }

        public static AppSettings CreateDefault()
        {
            return new AppSettings();
        }

        public string Validate()
        {
            Uri uri;
            if (string.IsNullOrWhiteSpace(TelemetryUrl) || !Uri.TryCreate(TelemetryUrl, UriKind.Absolute, out uri))
            {
                return "Telemetry URL must be a valid absolute URL.";
            }

            if (MinConfidence < 0d || MinConfidence > 1d)
            {
                return "Speech confidence must be between 0.00 and 1.00.";
            }

            if (InputSensitivityGate < 0d || InputSensitivityGate > 1d)
            {
                return "Input sensitivity must be between 0.00 and 1.00.";
            }

            if (OutputVolume < 0d || OutputVolume > 2d)
            {
                return "Output volume must be between 0.00 and 2.00.";
            }

            if (OutputPan < -1d || OutputPan > 1d)
            {
                return "Output pan must be between -1.00 and 1.00.";
            }

            if (string.IsNullOrWhiteSpace(OpenAiModel))
            {
                return "OpenAI model cannot be empty.";
            }

            if (string.IsNullOrWhiteSpace(OpenAiVoice))
            {
                return "OpenAI voice cannot be empty.";
            }

            return null;
        }
    }
}
