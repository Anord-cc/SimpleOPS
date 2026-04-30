using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SimpleOps.GsxRamp
{
    internal sealed class RampControlForm : Form
    {
        private const int WmUserSimConnect = 0x0402;
        private const string OpenAiCredentialKey = "SimpleOps.OpenAI.ApiKey";

        private readonly Options _options;
        private readonly AppPaths _appPaths;
        private readonly ISettingsStore _settingsStore;
        private readonly ICredentialStore _credentialStore;
        private readonly PhraseAliasStore _phraseAliasStore;
        private readonly AppLogger _logger;

        private AppSettings _settings;

        private Label _statusLabel;
        private Label _detailLabel;
        private Label _configLabel;
        private Label _systemStatusLabel;
        private TextBox _logBox;
        private Button _parserTestsButton;
        private Button _settingsButton;
        private Button _analyzePhraseButton;
        private TextBox _diagnosticPhraseTextBox;
        private TextBox _diagnosticResultTextBox;

        private GsxMenuDriver _gsxMenuDriver;
        private IGsxMenuController _menuController;
        private RampController _rampController;
        private RampPhraseParser _parser;
        private ISpeechInputService _speechInputService;
        private IVoiceOutputService _voiceOutputService;
        private Timer _autoCloseTimer;
        private Timer _statusRefreshTimer;

        public RampControlForm(Options options, AppPaths appPaths, ISettingsStore settingsStore, ICredentialStore credentialStore, PhraseAliasStore phraseAliasStore, AppSettings settings, AppLogger logger)
        {
            _options = options;
            _appPaths = appPaths;
            _settingsStore = settingsStore;
            _credentialStore = credentialStore;
            _phraseAliasStore = phraseAliasStore;
            _settings = settings;
            _logger = logger;

            Text = "SimpleOps";
            Width = 980;
            Height = 690;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ShowIcon = true;

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
            }

            BuildUi();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _logger.LineLogged += OnLineLogged;
            ReloadRuntime();

            if (_options.RunDurationSeconds > 0)
            {
                _autoCloseTimer = new Timer();
                _autoCloseTimer.Interval = _options.RunDurationSeconds * 1000;
                _autoCloseTimer.Tick += delegate
                {
                    _autoCloseTimer.Stop();
                    Close();
                };
                _autoCloseTimer.Start();
            }

            _statusRefreshTimer = new Timer();
            _statusRefreshTimer.Interval = 500;
            _statusRefreshTimer.Tick += delegate { RefreshStatusDisplay(); };
            _statusRefreshTimer.Start();
        }

        protected override void DefWndProc(ref Message m)
        {
            if (_gsxMenuDriver != null && _gsxMenuDriver.ProcessWindowMessage(ref m))
            {
                return;
            }

            base.DefWndProc(ref m);
        }

        protected override void OnClosed(EventArgs e)
        {
            _logger.LineLogged -= OnLineLogged;

            try
            {
                _statusRefreshTimer?.Stop();
                _statusRefreshTimer?.Dispose();
            }
            catch
            {
            }

            try
            {
                _autoCloseTimer?.Stop();
                _autoCloseTimer?.Dispose();
            }
            catch
            {
            }

            DisposeRuntime();
            base.OnClosed(e);
        }

        private void BuildUi()
        {
            _statusLabel = new Label
            {
                AutoSize = false,
                Left = 20,
                Top = 18,
                Width = 920,
                Height = 24,
                Text = "Starting..."
            };

            _detailLabel = new Label
            {
                AutoSize = false,
                Left = 20,
                Top = 50,
                Width = 920,
                Height = 44,
                Text = "Waiting for telemetry..."
            };

            _configLabel = new Label
            {
                AutoSize = false,
                Left = 20,
                Top = 98,
                Width = 920,
                Height = 46
            };

            _systemStatusLabel = new Label
            {
                AutoSize = false,
                Left = 20,
                Top = 150,
                Width = 920,
                Height = 90
            };

            _settingsButton = new Button
            {
                Left = 20,
                Top = 248,
                Width = 140,
                Height = 30,
                Text = "Settings"
            };
            _settingsButton.Click += SettingsButton_Click;

            _parserTestsButton = new Button
            {
                Left = 170,
                Top = 248,
                Width = 160,
                Height = 30,
                Text = "Run parser tests"
            };
            _parserTestsButton.Click += ParserTestsButton_Click;

            var diagnosticLabel = new Label
            {
                Left = 20,
                Top = 292,
                Width = 920,
                Height = 20,
                Text = "Phrase diagnostics"
            };

            _diagnosticPhraseTextBox = new TextBox
            {
                Left = 20,
                Top = 318,
                Width = 650,
                Height = 24
            };

            _analyzePhraseButton = new Button
            {
                Left = 680,
                Top = 316,
                Width = 120,
                Height = 28,
                Text = "Analyze"
            };
            _analyzePhraseButton.Click += AnalyzePhraseButton_Click;

            _diagnosticResultTextBox = new TextBox
            {
                Left = 20,
                Top = 350,
                Width = 920,
                Height = 90,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            _logBox = new TextBox
            {
                Left = 20,
                Top = 450,
                Width = 920,
                Height = 180,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true
            };

            Controls.Add(_statusLabel);
            Controls.Add(_detailLabel);
            Controls.Add(_configLabel);
            Controls.Add(_systemStatusLabel);
            Controls.Add(_settingsButton);
            Controls.Add(_parserTestsButton);
            Controls.Add(diagnosticLabel);
            Controls.Add(_diagnosticPhraseTextBox);
            Controls.Add(_analyzePhraseButton);
            Controls.Add(_diagnosticResultTextBox);
            Controls.Add(_logBox);
        }

        private void ReloadRuntime()
        {
            DisposeRuntime();

            _settings = _options.ApplyTo(_settingsStore.Load());
            _parser = new RampPhraseParser(_phraseAliasStore.Load());

            _speechInputService = _options.NoSpeech
                ? (ISpeechInputService)new DisabledSpeechInputService("Speech input disabled by command-line flag.")
                : CreateSpeechInputService();

            _voiceOutputService = _options.NoVoiceFeedback
                ? (IVoiceOutputService)new SilentVoiceOutputService("Voice output disabled by command-line flag.")
                : CreateVoiceOutputService();

            string gsxError;
            var gsxPaths = GsxPaths.TryDetect(out gsxError);
            if (gsxPaths != null)
            {
                _gsxMenuDriver = new GsxMenuDriver(gsxPaths, Handle, WmUserSimConnect, _logger.Log);
                _menuController = _gsxMenuDriver;
                _detailLabel.Text = "GSX panel: " + gsxPaths.GsxPanelPath;
            }
            else
            {
                _gsxMenuDriver = null;
                _menuController = new NullGsxMenuController(gsxError);
                _detailLabel.Text = "GSX panel unavailable.";
                _logger.Log(gsxError);
            }

            _configLabel.Text =
                "Telemetry: " + _settings.TelemetryUrl + Environment.NewLine +
                "Logs: " + _appPaths.LogDirectory;

            _rampController = new RampController(
                _options,
                _settings,
                new TelemetryClient(_settings.TelemetryUrl),
                _menuController,
                _parser,
                _speechInputService,
                _voiceOutputService,
                _logger.Log,
                SetStatus);
            _rampController.Start();
            RefreshStatusDisplay();
        }

        private ISpeechInputService CreateSpeechInputService()
        {
            try
            {
                return new LocalSpeechInputService(_settings, _logger.Log);
            }
            catch (Exception ex)
            {
                _logger.Log("Speech input initialization warning: " + ex.Message);
                return new DisabledSpeechInputService("Speech input unavailable: " + ex.Message);
            }
        }

        private IVoiceOutputService CreateVoiceOutputService()
        {
            try
            {
                return _settings.OpenAiVoiceEnabled
                    ? (IVoiceOutputService)new OpenAiVoiceOutputService(_settings, _credentialStore, _appPaths, _logger.Log)
                    : new SilentVoiceOutputService("OpenAI voice disabled in settings.");
            }
            catch (Exception ex)
            {
                _logger.Log("Voice output initialization warning: " + ex.Message);
                return new SilentVoiceOutputService("Voice output unavailable: " + ex.Message);
            }
        }

        private void DisposeRuntime()
        {
            try
            {
                _rampController?.Dispose();
            }
            catch
            {
            }

            try
            {
                _speechInputService?.Dispose();
            }
            catch
            {
            }

            try
            {
                _voiceOutputService?.Dispose();
            }
            catch
            {
            }

            try
            {
                _gsxMenuDriver?.Dispose();
            }
            catch
            {
            }

            _rampController = null;
            _speechInputService = null;
            _voiceOutputService = null;
            _gsxMenuDriver = null;
            _menuController = null;
        }

        private void RefreshStatusDisplay()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshStatusDisplay));
                return;
            }

            if (_rampController == null)
            {
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("Telemetry: " + _rampController.TelemetryStatusText);
            builder.AppendLine("Speech input: " + _rampController.SpeechInputStatusText);
            builder.AppendLine("OpenAI voice: " + _rampController.VoiceOutputStatusText);
            builder.AppendLine("GSX Remote: " + _rampController.GsxStatusText);
            builder.Append("Last command: " + _rampController.LastCommandText);
            _systemStatusLabel.Text = builder.ToString();
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new SettingsForm(
                _settings,
                _credentialStore.GetSecret(OpenAiCredentialKey),
                AudioDeviceCatalog.GetInputDevices(),
                AudioDeviceCatalog.GetOutputDevices()))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                _settingsStore.Save(dialog.UpdatedSettings);
                if (string.IsNullOrWhiteSpace(dialog.UpdatedApiKey))
                {
                    _credentialStore.DeleteSecret(OpenAiCredentialKey);
                }
                else
                {
                    _credentialStore.SaveSecret(OpenAiCredentialKey, dialog.UpdatedApiKey);
                }

                _logger.Log("Settings saved.");
                ReloadRuntime();
            }
        }

        private void ParserTestsButton_Click(object sender, EventArgs e)
        {
            int code = ParserTestHarness.Run(_logger.Log);
            MessageBox.Show(
                code == 0 ? "Parser tests passed." : "Parser tests failed. Review the log output for details.",
                "SimpleOps",
                MessageBoxButtons.OK,
                code == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void AnalyzePhraseButton_Click(object sender, EventArgs e)
        {
            if (_parser == null)
            {
                return;
            }

            var phrase = _diagnosticPhraseTextBox.Text ?? string.Empty;
            var command = _parser.Parse(phrase);
            var safeToExecute = command.IsSafeToExecute && (!command.IsActionableGsx || command.Quality == MatchQuality.Strong) && (_rampController != null && _rampController.IsArmed);
            _diagnosticResultTextBox.Text =
                "Raw: " + command.RawPhrase + Environment.NewLine +
                "Normalized: " + command.NormalizedPhrase + Environment.NewLine +
                "Intent: " + command.Type + Environment.NewLine +
                "Quality: " + command.Quality + Environment.NewLine +
                "Reason: " + command.Reason + Environment.NewLine +
                "GSX action allowed now: " + safeToExecute;
        }

        private void SetStatus(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(SetStatus), message);
                return;
            }

            _statusLabel.Text = message;
        }

        private void OnLineLogged(string line)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(OnLineLogged), line);
                return;
            }

            _logBox.AppendText(line + Environment.NewLine);
        }
    }
}
