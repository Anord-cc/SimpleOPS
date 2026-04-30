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

        private Label _heroStatusLabel;
        private Label _heroSummaryLabel;
        private Label _detailLabel;
        private Label _configLabel;
        private Label _systemStatusLabel;
        private RichTextBox _logBox;
        private Button _parserTestsButton;
        private Button _settingsButton;
        private Button _analyzePhraseButton;
        private TextBox _diagnosticPhraseTextBox;
        private RichTextBox _diagnosticResultTextBox;

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
            Width = 1320;
            Height = 860;
            MinimumSize = new Size(1180, 760);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UiTheme.WindowBackground;
            Font = UiTheme.BodyFont(9.75f);
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
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.WindowBackground,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(20, 18, 20, 20)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 168));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            root.Controls.Add(BuildHeroPanel(), 0, 0);
            root.Controls.Add(BuildBodyPanel(), 0, 1);

            Controls.Add(root);
        }

        private Control BuildHeroPanel()
        {
            var hero = new GradientPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 18),
                Padding = new Padding(26, 22, 26, 22)
            };

            var title = new Label
            {
                AutoSize = true,
                Text = "SimpleOps",
                Font = UiTheme.TitleFont(28f),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            var subtitle = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(650, 0),
                Text = "Ground operations command deck for MSFS with GSX remote control, live telemetry gating, and voice-driven service coordination.",
                Font = UiTheme.BodyFont(10.5f),
                ForeColor = Color.FromArgb(227, 241, 244),
                BackColor = Color.Transparent
            };

            _heroStatusLabel = new Label
            {
                AutoSize = false,
                Width = 360,
                Height = 34,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Starting...",
                Font = UiTheme.BodyFont(10f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                BackColor = Color.FromArgb(246, 240, 231),
                Padding = new Padding(12, 0, 0, 0)
            };

            _heroSummaryLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(700, 0),
                Text = "Booting command deck.",
                Font = UiTheme.BodyFont(10f),
                ForeColor = Color.FromArgb(240, 246, 247),
                BackColor = Color.Transparent
            };

            _settingsButton = new Button
            {
                Width = 126,
                Height = 38,
                Text = "Settings"
            };
            UiTheme.StylePrimaryButton(_settingsButton);
            _settingsButton.Click += SettingsButton_Click;

            _parserTestsButton = new Button
            {
                Width = 150,
                Height = 38,
                Text = "Run tests"
            };
            UiTheme.StyleSecondaryButton(_parserTestsButton);
            _parserTestsButton.ForeColor = Color.White;
            _parserTestsButton.BackColor = Color.FromArgb(37, 110, 118);
            _parserTestsButton.FlatAppearance.BorderColor = Color.FromArgb(105, 171, 176);
            _parserTestsButton.Click += ParserTestsButton_Click;

            var left = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.Controls.Add(title, 0, 0);
            left.Controls.Add(subtitle, 0, 1);
            left.Controls.Add(new Panel { Height = 12, Width = 1, BackColor = Color.Transparent }, 0, 2);
            left.Controls.Add(_heroSummaryLabel, 0, 3);

            var rightButtons = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            rightButtons.Controls.Add(_settingsButton);
            rightButtons.Controls.Add(_parserTestsButton);

            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.Controls.Add(rightButtons, 0, 0);
            right.Controls.Add(new Panel { BackColor = Color.Transparent }, 0, 1);
            right.Controls.Add(_heroStatusLabel, 0, 2);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            layout.Controls.Add(left, 0, 0);
            layout.Controls.Add(right, 1, 0);

            hero.Controls.Add(layout);
            return hero;
        }

        private Control BuildBodyPanel()
        {
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = UiTheme.WindowBackground
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 400));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var leftColumn = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0, 0, 18, 0),
                BackColor = UiTheme.WindowBackground
            };
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Absolute, 154));
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _detailLabel = CreateBodyLabel();
            _configLabel = CreateBodyLabel();
            _systemStatusLabel = CreateBodyLabel();

            leftColumn.Controls.Add(BuildInfoCard("Flight Link", "Telemetry path, GSX location, and runtime environment.", _detailLabel, _configLabel), 0, 0);
            leftColumn.Controls.Add(BuildInfoCard("Runtime Status", "Live system health across telemetry, speech, voice, and GSX.", _systemStatusLabel), 0, 1);
            leftColumn.Controls.Add(BuildDiagnosticsCard(), 0, 2);

            body.Controls.Add(leftColumn, 0, 0);
            body.Controls.Add(BuildConsoleCard(), 1, 0);
            return body;
        }

        private Control BuildInfoCard(string title, string subtitle, params Control[] content)
        {
            var card = new CardPanel { Dock = DockStyle.Fill };
            var layout = CreateCardLayout(title, subtitle);
            int row = 2;
            for (int i = 0; i < content.Length; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.Controls.Add(content[i], 0, row++);
            }

            card.Controls.Add(layout);
            return card;
        }

        private Control BuildDiagnosticsCard()
        {
            var card = new CardPanel { Dock = DockStyle.Fill };
            var layout = CreateCardLayout("Phrase Diagnostics", "Test pilot-to-ramp phrases safely before sending any live service command.");

            _diagnosticPhraseTextBox = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 32,
                BorderStyle = BorderStyle.FixedSingle
            };
            UiTheme.StyleInput(_diagnosticPhraseTextBox);

            _analyzePhraseButton = new Button
            {
                Width = 118,
                Height = 34,
                Text = "Analyze"
            };
            UiTheme.StylePrimaryButton(_analyzePhraseButton);
            _analyzePhraseButton.Click += AnalyzePhraseButton_Click;

            _diagnosticResultTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                BackColor = Color.FromArgb(251, 247, 240),
                ForeColor = UiTheme.TextPrimary,
                Font = UiTheme.BodyFont(9.5f)
            };

            var buttonRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.Transparent
            };
            buttonRow.Controls.Add(_analyzePhraseButton);

            var resultHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(251, 247, 240),
                Padding = new Padding(12)
            };
            resultHost.Controls.Add(_diagnosticResultTextBox);

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.Controls.Add(_diagnosticPhraseTextBox, 0, 2);
            layout.Controls.Add(buttonRow, 0, 3);
            layout.Controls.Add(resultHost, 0, 4);

            card.Controls.Add(layout);
            return card;
        }

        private Control BuildConsoleCard()
        {
            var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            var layout = CreateCardLayout("Operations Console", "Live command log, service acknowledgements, and startup diagnostics.");

            _logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                BackColor = Color.FromArgb(29, 33, 40),
                ForeColor = Color.FromArgb(233, 240, 243),
                Font = new Font("Consolas", 9.5f, FontStyle.Regular, GraphicsUnit.Point)
            };

            var chrome = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(29, 33, 40),
                Padding = new Padding(16)
            };
            chrome.Controls.Add(_logBox);

            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.Controls.Add(chrome, 0, 2);
            card.Controls.Add(layout);
            return card;
        }

        private TableLayoutPanel CreateCardLayout(string title, string subtitle)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = UiTheme.CardBackground
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var titleLabel = new Label
            {
                AutoSize = true,
                Text = title,
                Font = UiTheme.TitleFont(14f),
                ForeColor = UiTheme.TextPrimary
            };

            var subtitleLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(760, 0),
                Margin = new Padding(0, 6, 0, 16),
                Text = subtitle,
                Font = UiTheme.BodyFont(9.75f),
                ForeColor = UiTheme.TextMuted
            };

            layout.Controls.Add(titleLabel, 0, 0);
            layout.Controls.Add(subtitleLabel, 0, 1);
            return layout;
        }

        private static Label CreateBodyLabel()
        {
            return new Label
            {
                AutoSize = true,
                MaximumSize = new Size(720, 0),
                Margin = new Padding(0, 0, 0, 12),
                Font = UiTheme.BodyFont(9.75f),
                ForeColor = UiTheme.TextPrimary
            };
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

            _heroSummaryLabel.Text = _rampController.IsArmed
                ? "Ramp channel is armed. Ground services can be dispatched when a strong phrase match is detected."
                : "Ramp channel is standing by. The command deck stays passive until the aircraft is confirmed on the ground.";
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

            _heroStatusLabel.Text = message;
        }

        private void OnLineLogged(string line)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(OnLineLogged), line);
                return;
            }

            _logBox.AppendText(line + Environment.NewLine);
            _logBox.SelectionStart = _logBox.TextLength;
            _logBox.ScrollToCaret();
        }
    }
}
