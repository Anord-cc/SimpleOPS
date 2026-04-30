// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
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

        private Button _overviewNavButton;
        private Button _settingsNavButton;
        private Label _pageTitleLabel;
        private Label _pageSubtitleLabel;
        private Panel _overviewPage;
        private SettingsPanel _settingsPanel;
        private Label _heroStatusLabel;
        private Label _armedSummaryLabel;
        private TextBox _telemetryReadout;
        private TextBox _gsxReadout;
        private TextBox _voiceReadout;
        private TextBox _lastCommandReadout;
        private TextBox _runtimeReadout;

        private GsxMenuDriver _gsxMenuDriver;
        private IGsxMenuController _menuController;
        private RampController _rampController;
        private RampPhraseParser _parser;
        private ISpeechInputService _speechInputService;
        private IVoiceOutputService _voiceOutputService;
        private Timer _autoCloseTimer;
        private Timer _statusRefreshTimer;
        private string _activePage = "Overview";

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
            Width = 1340;
            Height = 860;
            MinimumSize = new Size(1240, 780);
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
                ColumnCount = 2,
                RowCount = 1,
                BackColor = UiTheme.WindowBackground
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            root.Controls.Add(BuildNavigationRail(), 0, 0);
            root.Controls.Add(BuildMainArea(), 1, 0);
            Controls.Add(root);
        }

        private Control BuildNavigationRail()
        {
            var rail = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.RailBackground,
                Padding = new Padding(18, 24, 18, 24)
            };

            var title = new Label
            {
                AutoSize = true,
                Text = "SimpleOps",
                Font = UiTheme.TitleFont(24f),
                ForeColor = UiTheme.TextPrimary
            };

            var subtitle = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(170, 0),
                Top = 56,
                Text = "Voice command deck",
                Font = UiTheme.BodyFont(9.75f),
                ForeColor = UiTheme.TextMuted
            };

            _overviewNavButton = new Button
            {
                Width = 182,
                Height = 44,
                Top = 122,
                Left = 0,
                Text = "Overview"
            };
            _overviewNavButton.Click += delegate { ShowPage("Overview"); };

            _settingsNavButton = new Button
            {
                Width = 182,
                Height = 44,
                Top = 174,
                Left = 0,
                Text = "Settings"
            };
            _settingsNavButton.Click += delegate { ShowPage("Settings"); };

            var statusHint = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(170, 0),
                Top = 260,
                Text = "Advanced lives inside Settings.",
                Font = UiTheme.BodyFont(9f),
                ForeColor = UiTheme.TextSoft
            };

            rail.Controls.Add(title);
            rail.Controls.Add(subtitle);
            rail.Controls.Add(_overviewNavButton);
            rail.Controls.Add(_settingsNavButton);
            rail.Controls.Add(statusHint);
            return rail;
        }

        private Control BuildMainArea()
        {
            var shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = UiTheme.WindowBackground,
                Padding = new Padding(24, 22, 24, 22)
            };
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            shell.Controls.Add(BuildPageHeader(), 0, 0);
            shell.Controls.Add(BuildContentHost(), 0, 1);
            return shell;
        }

        private Control BuildPageHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.WindowBackground
            };

            _pageTitleLabel = new Label
            {
                AutoSize = true,
                Text = "Overview",
                Font = UiTheme.TitleFont(26f),
                ForeColor = UiTheme.TextPrimary
            };

            _pageSubtitleLabel = new Label
            {
                AutoSize = true,
                Top = 42,
                MaximumSize = new Size(760, 0),
                Text = "Live aircraft, GSX, telemetry, and voice status.",
                Font = UiTheme.BodyFont(9.75f),
                ForeColor = UiTheme.TextMuted
            };

            header.Controls.Add(_pageTitleLabel);
            header.Controls.Add(_pageSubtitleLabel);
            return header;
        }

        private Control BuildContentHost()
        {
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.WindowBackground
            };

            _overviewPage = BuildOverviewPage();
            _settingsPanel = new SettingsPanel { Visible = false };
            _settingsPanel.SaveRequested += SettingsPanel_SaveRequested;
            _settingsPanel.AnalyzePhraseRequested += SettingsPanel_AnalyzePhraseRequested;
            _settingsPanel.RunParserTestsRequested += SettingsPanel_RunParserTestsRequested;

            host.Controls.Add(_overviewPage);
            host.Controls.Add(_settingsPanel);
            return host;
        }

        private Panel BuildOverviewPage()
        {
            var page = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.WindowBackground
            };

            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = UiTheme.WindowBackground
            };
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 138));
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 138));
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 138));
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            stack.Controls.Add(BuildStatusCard(), 0, 0);
            stack.Controls.Add(BuildConnectionCard(), 0, 1);
            stack.Controls.Add(BuildVoiceCard(), 0, 2);
            stack.Controls.Add(BuildRuntimeCard(), 0, 3);

            page.Controls.Add(stack);
            return page;
        }

        private Control BuildStatusCard()
        {
            var card = new CardPanel { Dock = DockStyle.Fill };
            var layout = CreateCardLayout("Command Status", "Short live readout. No clipped hero copy.");

            _heroStatusLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(820, 0),
                Text = "Starting...",
                Font = UiTheme.TitleFont(18f),
                ForeColor = UiTheme.TextPrimary
            };

            _armedSummaryLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(820, 0),
                Margin = new Padding(0, 8, 0, 0),
                Text = "Waiting for telemetry.",
                Font = UiTheme.BodyFont(10f),
                ForeColor = UiTheme.TextMuted
            };

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(_heroStatusLabel, 0, 2);
            layout.Controls.Add(_armedSummaryLabel, 0, 3);
            card.Controls.Add(layout);
            return card;
        }

        private Control BuildConnectionCard()
        {
            var card = new CardPanel { Dock = DockStyle.Fill };
            var layout = CreateCardLayout("Links", "Telemetry and GSX paths stay readable in fixed-width fields.");

            _telemetryReadout = CreateReadoutTextBox();
            _gsxReadout = CreateReadoutTextBox();

            AddReadout(layout, 2, "Telemetry", _telemetryReadout);
            AddReadout(layout, 3, "GSX panel", _gsxReadout);
            card.Controls.Add(layout);
            return card;
        }

        private Control BuildVoiceCard()
        {
            var card = new CardPanel { Dock = DockStyle.Fill };
            var layout = CreateCardLayout("Voice + Input", "Current speech and voice pipeline state.");

            _voiceReadout = CreateMultilineReadout();
            AddReadout(layout, 2, "Voice state", _voiceReadout);
            card.Controls.Add(layout);
            return card;
        }

        private Control BuildRuntimeCard()
        {
            var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            var layout = CreateCardLayout("Runtime", "Last command and compact service state.");

            _lastCommandReadout = CreateReadoutTextBox();
            _runtimeReadout = CreateMultilineReadout();

            AddReadout(layout, 2, "Last command", _lastCommandReadout);
            AddReadout(layout, 3, "Service state", _runtimeReadout);
            card.Controls.Add(layout);
            return card;
        }

        private TableLayoutPanel CreateCardLayout(string title, string subtitle)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = UiTheme.CardBackground
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var titleLabel = new Label
            {
                AutoSize = true,
                Text = title,
                Font = UiTheme.TitleFont(15f),
                ForeColor = UiTheme.TextPrimary
            };

            var subtitleLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(820, 0),
                Margin = new Padding(0, 6, 0, 16),
                Text = subtitle,
                Font = UiTheme.BodyFont(9.5f),
                ForeColor = UiTheme.TextMuted
            };

            layout.SetColumnSpan(titleLabel, 2);
            layout.SetColumnSpan(subtitleLabel, 2);
            layout.Controls.Add(titleLabel, 0, 0);
            layout.Controls.Add(subtitleLabel, 0, 1);
            return layout;
        }

        private void AddReadout(TableLayoutPanel layout, int row, string label, Control control)
        {
            if (layout.RowCount <= row)
            {
                layout.RowCount = row + 1;
            }

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var labelControl = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 0),
                Text = label,
                Font = UiTheme.BodyFont(9.5f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary
            };

            control.Margin = new Padding(0, 4, 0, 8);
            layout.Controls.Add(labelControl, 0, row);
            layout.Controls.Add(control, 1, row);
        }

        private static TextBox CreateReadoutTextBox()
        {
            var textBox = new TextBox
            {
                Width = 860,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            UiTheme.StyleInput(textBox);
            return textBox;
        }

        private static TextBox CreateMultilineReadout()
        {
            var textBox = new TextBox
            {
                Width = 860,
                Height = 62,
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical
            };
            UiTheme.StyleInput(textBox);
            return textBox;
        }

        private void ReloadRuntime()
        {
            DisposeRuntime();

            _settings = _options.ApplyTo(_settingsStore.Load());
            _parser = new RampPhraseParser(_phraseAliasStore.Load());

            _speechInputService = _options.NoSpeech
                ? (ISpeechInputService)new DisabledSpeechInputService("Speech input disabled.")
                : CreateSpeechInputService();

            _voiceOutputService = _options.NoVoiceFeedback
                ? (IVoiceOutputService)new SilentVoiceOutputService("Voice output disabled.")
                : CreateVoiceOutputService();

            string gsxError;
            var gsxPaths = GsxPaths.TryDetect(out gsxError);
            if (gsxPaths != null)
            {
                _gsxMenuDriver = new GsxMenuDriver(gsxPaths, Handle, WmUserSimConnect, _logger.Log);
                _menuController = _gsxMenuDriver;
                _gsxReadout.Text = gsxPaths.GsxPanelPath;
            }
            else
            {
                _gsxMenuDriver = null;
                _menuController = new NullGsxMenuController(gsxError);
                _gsxReadout.Text = "Unavailable";
                _logger.Log(gsxError);
            }

            _telemetryReadout.Text = _settings.TelemetryUrl;

            _settingsPanel.Bind(
                _settings,
                _credentialStore.GetSecret(OpenAiCredentialKey),
                AudioDeviceCatalog.GetInputDevices(),
                AudioDeviceCatalog.GetOutputDevices());

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
                _logger.Log("Speech input warning: " + ex.Message);
                return new DisabledSpeechInputService("Speech input unavailable.");
            }
        }

        private IVoiceOutputService CreateVoiceOutputService()
        {
            try
            {
                return _settings.OpenAiVoiceEnabled
                    ? (IVoiceOutputService)new OpenAiVoiceOutputService(_settings, _credentialStore, _appPaths, _logger.Log)
                    : new SilentVoiceOutputService("OpenAI voice disabled.");
            }
            catch (Exception ex)
            {
                _logger.Log("Voice output warning: " + ex.Message);
                return new SilentVoiceOutputService("Voice output unavailable.");
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

            _armedSummaryLabel.Text = _rampController.IsArmed ? "Ramp active. Aircraft confirmed on ground." : "Ramp standby. Waiting for on-ground telemetry.";
            _voiceReadout.Text =
                "Speech: " + _rampController.SpeechInputStatusText + Environment.NewLine +
                "Voice: " + _rampController.VoiceOutputStatusText;
            _lastCommandReadout.Text = _rampController.LastCommandText;
            _runtimeReadout.Text =
                "Telemetry: " + _rampController.TelemetryStatusText + Environment.NewLine +
                "GSX: " + _rampController.GsxStatusText;
        }

        private void ShowPage(string pageName)
        {
            _activePage = pageName;
            bool showOverview = string.Equals(pageName, "Overview", StringComparison.OrdinalIgnoreCase);

            _overviewPage.Visible = showOverview;
            _settingsPanel.Visible = !showOverview;

            _pageTitleLabel.Text = showOverview ? "Overview" : "Settings";
            _pageSubtitleLabel.Text = showOverview
                ? "Live aircraft, GSX, telemetry, and voice status."
                : "Inline controls plus Advanced diagnostics and console.";

            UiTheme.StyleNavButton(_overviewNavButton, showOverview);
            UiTheme.StyleNavButton(_settingsNavButton, !showOverview);
        }

        private void SettingsPanel_SaveRequested(object sender, EventArgs e)
        {
            if (_settingsPanel.PendingSettings == null)
            {
                return;
            }

            _settingsStore.Save(_settingsPanel.PendingSettings);
            if (string.IsNullOrWhiteSpace(_settingsPanel.PendingApiKey))
            {
                _credentialStore.DeleteSecret(OpenAiCredentialKey);
            }
            else
            {
                _credentialStore.SaveSecret(OpenAiCredentialKey, _settingsPanel.PendingApiKey);
            }

            _logger.Log("Settings saved.");
            ReloadRuntime();
            ShowPage("Settings");
        }

        private void SettingsPanel_AnalyzePhraseRequested(object sender, EventArgs e)
        {
            if (_parser == null)
            {
                return;
            }

            var phrase = _settingsPanel.DiagnosticPhrase;
            var command = _parser.Parse(phrase);
            var safeToExecute = command.IsSafeToExecute && (!command.IsActionableGsx || command.Quality == MatchQuality.Strong) && (_rampController != null && _rampController.IsArmed);
            _settingsPanel.SetDiagnosticResult(
                "Raw: " + command.RawPhrase + Environment.NewLine +
                "Normalized: " + command.NormalizedPhrase + Environment.NewLine +
                "Intent: " + command.Type + Environment.NewLine +
                "Quality: " + command.Quality + Environment.NewLine +
                "Reason: " + command.Reason + Environment.NewLine +
                "GSX action allowed: " + safeToExecute);
        }

        private void SettingsPanel_RunParserTestsRequested(object sender, EventArgs e)
        {
            int code = ParserTestHarness.Run(_logger.Log);
            MessageBox.Show(
                code == 0 ? "Parser tests passed." : "Parser tests failed. Review the console in Advanced.",
                "SimpleOps",
                MessageBoxButtons.OK,
                code == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
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

            _settingsPanel.AppendLog(line);
        }
    }
}
