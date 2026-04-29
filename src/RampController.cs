using System;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading;

namespace SimpleOps.GsxRamp
{
    internal sealed class RampController : IDisposable
    {
        private readonly Options _options;
        private readonly TelemetryClient _telemetryClient;
        private readonly GsxPaths _paths;
        private readonly IGsxMenuController _menuController;
        private readonly RampPhraseParser _parser;
        private readonly RampCommandProcessor _processor;
        private readonly object _actionLock = new object();

        private SpeechRecognitionEngine _recognizer;
        private SpeechSynthesizer _synthesizer;
        private bool _armed;
        private string _lastStatus;

        public RampController(Options options)
            : this(options, new TelemetryClient(options.TelemetryUrl), CreateMenuController(), new RampPhraseParser())
        {
        }

        internal RampController(Options options, TelemetryClient telemetryClient, IGsxMenuController menuController, RampPhraseParser parser)
        {
            _options = options;
            _telemetryClient = telemetryClient;
            _menuController = menuController;
            _parser = parser;
            _processor = new RampCommandProcessor(_menuController, _options.DryRun, Log);
            _paths = GsxPaths.Detect();
        }

        public void Start()
        {
            Log("SimpleOps GSX ramp controller");
            Log("Telemetry: " + _options.TelemetryUrl);
            Log("GSX panel: " + _paths.GsxPanelPath);
            InitializeSpeech();
            UpdateTelemetryState();
        }

        public void RunLoop()
        {
            DateTime? endAt = null;
            if (_options.RunDurationSeconds > 0)
            {
                endAt = DateTime.UtcNow.AddSeconds(_options.RunDurationSeconds);
            }

            if (!string.IsNullOrWhiteSpace(_options.TestPhrase))
            {
                HandlePhrase(_options.TestPhrase);
                if (!endAt.HasValue)
                {
                    return;
                }
            }

            while (!endAt.HasValue || DateTime.UtcNow < endAt.Value)
            {
                UpdateTelemetryState();
                Thread.Sleep(1000);
            }
        }

        private void InitializeSpeech()
        {
            if (_options.NoSpeech)
            {
                Log("Speech recognition disabled.");
                return;
            }

            var culture = new System.Globalization.CultureInfo("en-US");
            _recognizer = new SpeechRecognitionEngine(culture);
            _recognizer.SetInputToDefaultAudioDevice();

            var choices = new Choices();
            ParserTestHarness.AddAllRecognizedPhrases(choices);

            var builder = new GrammarBuilder();
            builder.Culture = culture;
            builder.Append(choices);

            var grammar = new Grammar(builder);
            grammar.Name = "SimpleOpsRampGrammar";
            _recognizer.LoadGrammar(grammar);
            _recognizer.SpeechRecognized += OnSpeechRecognized;
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
            Log("Speech recognition armed.");
        }

        private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result == null || e.Result.Confidence < _options.MinConfidence)
            {
                Log("Speech phrase rejected due to engine confidence.");
                return;
            }

            Log(string.Format("Recognized '{0}' ({1:P0})", e.Result.Text, e.Result.Confidence));
            try
            {
                HandlePhrase(e.Result.Text);
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
                Log("Telemetry error: " + ex.Message);
            }
        }

        private void HandlePhrase(string phrase)
        {
            lock (_actionLock)
            {
                var command = _parser.Parse(phrase);
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
            Speak(message);
        }

        private void Speak(string message)
        {
            Log(message);

            if (_options.NoVoiceFeedback)
            {
                return;
            }

            if (_synthesizer == null)
            {
                _synthesizer = new SpeechSynthesizer();
                _synthesizer.SetOutputToDefaultAudioDevice();
                _synthesizer.Rate = 1;
            }

            _synthesizer.SpeakAsyncCancelAll();
            _synthesizer.SpeakAsync(message);
        }

        private static void Log(string message)
        {
            Console.WriteLine("[{0}] {1}", DateTime.Now.ToString("HH:mm:ss"), message);
        }

        private static IGsxMenuController CreateMenuController()
        {
            var paths = GsxPaths.Detect();
            var hotkeySender = new GsxHotkeySender(paths);
            return new GsxMenuDriver(paths, hotkeySender);
        }

        public void Dispose()
        {
            try
            {
                if (_recognizer != null)
                {
                    _recognizer.RecognizeAsyncCancel();
                    _recognizer.RecognizeAsyncStop();
                    _recognizer.Dispose();
                }
            }
            catch
            {
            }

            try
            {
                if (_synthesizer != null)
                {
                    _synthesizer.SpeakAsyncCancelAll();
                    _synthesizer.Dispose();
                }
            }
            catch
            {
            }
        }
    }
}
