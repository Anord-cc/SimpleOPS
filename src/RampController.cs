// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
using System;
using System.Threading;

namespace SimpleOps.GsxRamp
{
    internal sealed class RampController : IDisposable
    {
        private readonly Options _options;
        private readonly AppSettings _settings;
        private readonly ITelemetryClient _telemetryClient;
        private readonly IGsxMenuController _menuController;
        private readonly RampPhraseParser _parser;
        private readonly RampCommandProcessor _processor;
        private readonly ISpeechInputService _speechInputService;
        private readonly IVoiceOutputService _voiceOutputService;
        private readonly Action<string> _log;
        private readonly Action<string> _status;
        private readonly object _actionLock = new object();

        private Timer _pollTimer;
        private bool _armed;
        private string _lastStatus;

        public RampController(
            Options options,
            AppSettings settings,
            ITelemetryClient telemetryClient,
            IGsxMenuController menuController,
            RampPhraseParser parser,
            ISpeechInputService speechInputService,
            IVoiceOutputService voiceOutputService,
            Action<string> log,
            Action<string> status)
        {
            _options = options;
            _settings = settings ?? AppSettings.CreateDefault();
            _telemetryClient = telemetryClient;
            _menuController = menuController;
            _parser = parser;
            _speechInputService = speechInputService;
            _voiceOutputService = voiceOutputService;
            _processor = new RampCommandProcessor(_menuController, _settings.DryRun, Log);
            _log = log ?? delegate { };
            _status = status ?? delegate { };
            TelemetryStatusText = "Waiting for telemetry...";
            LastCommandText = "No commands yet.";
        }

        public bool IsArmed
        {
            get { return _armed; }
        }

        public string TelemetryStatusText { get; private set; }

        public string LastCommandText { get; private set; }

        public string SpeechInputStatusText
        {
            get { return _speechInputService == null ? "Speech input unavailable." : _speechInputService.StatusText; }
        }

        public string VoiceOutputStatusText
        {
            get { return _voiceOutputService == null ? "Voice output unavailable." : _voiceOutputService.StatusText; }
        }

        public string GsxStatusText
        {
            get { return _menuController == null ? "GSX unavailable." : _menuController.StatusText; }
        }

        public void Start()
        {
            Log("SimpleOps desktop controller");
            Log("Telemetry: " + _settings.TelemetryUrl);
            if (_settings.DryRun)
            {
                Log("Dry-run mode is enabled.");
            }

            InitializeSpeech();
            UpdateTelemetryState();
            _pollTimer = new Timer(PollTelemetry, null, 1000, 1000);

            if (!string.IsNullOrWhiteSpace(_options.TestPhrase))
            {
                ThreadPool.QueueUserWorkItem(delegate { HandlePhrase(_options.TestPhrase); });
            }
        }

        public RampCommand AnalyzePhrase(string phrase)
        {
            return _parser.Parse(phrase);
        }

        private void PollTelemetry(object state)
        {
            UpdateTelemetryState();
        }

        private void InitializeSpeech()
        {
            if (_options.NoSpeech || _speechInputService == null)
            {
                Log("Speech recognition disabled.");
                return;
            }

            _speechInputService.Start(_parser.GetAllRecognizedPhrases(), OnRecognizedPhrase);
            Log("Speech recognition armed.");
        }

        private void OnRecognizedPhrase(RecognizedPhrase phrase)
        {
            if (phrase == null || string.IsNullOrWhiteSpace(phrase.Text))
            {
                return;
            }

            if (phrase.Confidence < _settings.MinConfidence)
            {
                Log("Speech phrase rejected due to engine confidence.");
                return;
            }

            if (phrase.AudioLevel < _settings.InputSensitivityGate)
            {
                Log("Speech phrase rejected due to input sensitivity gate.");
                return;
            }

            Log(string.Format("Recognized '{0}' ({1:P0}) level={2:0.00}", phrase.Text, phrase.Confidence, phrase.AudioLevel));
            try
            {
                HandlePhrase(phrase.Text);
            }
            catch (Exception ex)
            {
                Speak("GSX command failed.");
                Log("Error: " + ex.Message);
            }
        }

        private void UpdateTelemetryState()
        {
            try
            {
                var snapshot = _telemetryClient.GetSnapshot();
                var armed = snapshot != null && snapshot.OnGround && snapshot.Connected && snapshot.Online;
                TelemetryStatusText = BuildTelemetryStatus(snapshot, armed);
                if (armed != _armed)
                {
                    _armed = armed;
                    if (_armed)
                    {
                        AnnounceStatus("Aircraft is on the ground. Ramp is active.");
                    }
                    else
                    {
                        AnnounceStatus("Ramp is inactive until the aircraft is on the ground.");
                    }
                }
            }
            catch (Exception ex)
            {
                TelemetryStatusText = "Telemetry warning: " + ex.Message;
                Log(TelemetryStatusText);
            }
        }

        private static string BuildTelemetryStatus(TelemetrySnapshot snapshot, bool armed)
        {
            if (snapshot == null)
            {
                return "Telemetry unavailable.";
            }

            return string.Format(
                "Telemetry online={0} connected={1} onGround={2} armed={3}",
                snapshot.Online,
                snapshot.Connected,
                snapshot.OnGround,
                armed);
        }

        private void HandlePhrase(string phrase)
        {
            lock (_actionLock)
            {
                var command = _parser.Parse(phrase);
                LastCommandText = command.Type + " from '" + command.RawPhrase + "' (" + command.Quality + ")";
                Log("Parsed '" + command.RawPhrase + "' => " + command.Type + " (" + command.Quality + "). " + command.Reason);

                bool armed = _armed || !string.IsNullOrWhiteSpace(_options.TestPhrase);
                var response = _processor.Execute(command, armed);
                if (!string.IsNullOrWhiteSpace(response))
                {
                    AnnounceStatus(response);
                }
            }
        }

        private void AnnounceStatus(string message)
        {
            if (string.Equals(_lastStatus, message, StringComparison.Ordinal))
            {
                return;
            }

            _lastStatus = message;
            _status(message);
            Speak(message);
        }

        private void Speak(string message)
        {
            Log(message);

            if (_options.NoVoiceFeedback || _voiceOutputService == null)
            {
                return;
            }

            _voiceOutputService.SpeakAsync(message);
        }

        private void Log(string message)
        {
            _log(message);
        }

        public void Dispose()
        {
            try
            {
                _pollTimer?.Dispose();
            }
            catch
            {
            }

            try
            {
                _speechInputService?.Stop();
            }
            catch
            {
            }
        }
    }
}
