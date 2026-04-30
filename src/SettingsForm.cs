using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SimpleOps.GsxRamp
{
    internal sealed class SettingsPanel : UserControl
    {
        private IList<AudioInputDeviceInfo> _inputDevices = new List<AudioInputDeviceInfo>();
        private IList<AudioOutputDeviceInfo> _outputDevices = new List<AudioOutputDeviceInfo>();

        private TextBox _telemetryUrlTextBox;
        private NumericUpDown _minConfidenceUpDown;
        private ComboBox _inputDeviceComboBox;
        private ComboBox _outputDeviceComboBox;
        private NumericUpDown _inputSensitivityUpDown;
        private NumericUpDown _outputVolumeUpDown;
        private ComboBox _outputChannelComboBox;
        private NumericUpDown _outputPanUpDown;
        private CheckBox _dryRunCheckBox;
        private CheckBox _openAiVoiceEnabledCheckBox;
        private ComboBox _openAiModelComboBox;
        private ComboBox _openAiVoiceComboBox;
        private TextBox _apiKeyTextBox;
        private Label _validationLabel;
        private TextBox _diagnosticPhraseTextBox;
        private RichTextBox _diagnosticResultTextBox;
        private RichTextBox _logBox;

        public SettingsPanel()
        {
            Dock = DockStyle.Fill;
            BackColor = UiTheme.WindowBackground;
            Font = UiTheme.BodyFont(9.75f);
            BuildUi();
        }

        public event EventHandler SaveRequested;

        public event EventHandler AnalyzePhraseRequested;

        public event EventHandler RunParserTestsRequested;

        public AppSettings PendingSettings { get; private set; }

        public string PendingApiKey { get; private set; }

        public string DiagnosticPhrase
        {
            get { return _diagnosticPhraseTextBox.Text ?? string.Empty; }
        }

        public void Bind(AppSettings settings, string apiKey, IList<AudioInputDeviceInfo> inputDevices, IList<AudioOutputDeviceInfo> outputDevices)
        {
            _inputDevices = inputDevices ?? new List<AudioInputDeviceInfo>();
            _outputDevices = outputDevices ?? new List<AudioOutputDeviceInfo>();

            FillCombo(_inputDeviceComboBox, _inputDevices.Select(device => device.Name));
            FillCombo(_outputDeviceComboBox, _outputDevices.Select(device => device.Name));

            PendingSettings = (settings ?? AppSettings.CreateDefault()).Clone();
            PendingApiKey = apiKey ?? string.Empty;

            _telemetryUrlTextBox.Text = PendingSettings.TelemetryUrl ?? string.Empty;
            _minConfidenceUpDown.Value = ClampDecimal((decimal)PendingSettings.MinConfidence, _minConfidenceUpDown.Minimum, _minConfidenceUpDown.Maximum);
            _inputSensitivityUpDown.Value = ClampDecimal((decimal)PendingSettings.InputSensitivityGate, _inputSensitivityUpDown.Minimum, _inputSensitivityUpDown.Maximum);
            _outputVolumeUpDown.Value = ClampDecimal((decimal)PendingSettings.OutputVolume, _outputVolumeUpDown.Minimum, _outputVolumeUpDown.Maximum);
            _outputPanUpDown.Value = ClampDecimal((decimal)PendingSettings.OutputPan, _outputPanUpDown.Minimum, _outputPanUpDown.Maximum);
            _dryRunCheckBox.Checked = PendingSettings.DryRun;
            _openAiVoiceEnabledCheckBox.Checked = PendingSettings.OpenAiVoiceEnabled;
            _openAiModelComboBox.Text = PendingSettings.OpenAiModel ?? "gpt-4o-mini-tts";
            _openAiVoiceComboBox.Text = PendingSettings.OpenAiVoice ?? "marin";
            _outputChannelComboBox.Text = PendingSettings.OutputChannel.ToString();
            _apiKeyTextBox.Text = PendingApiKey;
            _inputDeviceComboBox.Text = PendingSettings.MicrophoneDeviceName ?? string.Empty;
            _outputDeviceComboBox.Text = PendingSettings.SpeakerDeviceName ?? string.Empty;
            _validationLabel.Text = string.Empty;
            _diagnosticResultTextBox.Text = string.Empty;
        }

        public void SetDiagnosticResult(string text)
        {
            _diagnosticResultTextBox.Text = text ?? string.Empty;
        }

        public void AppendLog(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            _logBox.AppendText(line + Environment.NewLine);
            _logBox.SelectionStart = _logBox.TextLength;
            _logBox.ScrollToCaret();
        }

        private void BuildUi()
        {
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = UiTheme.WindowBackground
            };

            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 5,
                Width = 940,
                BackColor = UiTheme.WindowBackground,
                Padding = new Padding(0, 0, 10, 0)
            };

            stack.Controls.Add(BuildIntroCard(), 0, 0);
            stack.Controls.Add(BuildFlightCard(), 0, 1);
            stack.Controls.Add(BuildRecognitionCard(), 0, 2);
            stack.Controls.Add(BuildAudioVoiceCard(), 0, 3);
            stack.Controls.Add(BuildAdvancedCard(), 0, 4);

            scroll.Controls.Add(stack);
            Controls.Add(scroll);
        }

        private Control BuildIntroCard()
        {
            var card = new CardPanel { Dock = DockStyle.Top, Height = 122 };
            var layout = CreateFormCard("Settings", "Everything stays in this window. Save here, then use Advanced for diagnostics and logs.");
            _validationLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(860, 0),
                ForeColor = Color.White,
                Font = UiTheme.BodyFont(9.5f, FontStyle.Bold)
            };

            var saveButton = new Button
            {
                Text = "Save Changes",
                Width = 140,
                Height = 36
            };
            UiTheme.StylePrimaryButton(saveButton);
            saveButton.Click += SaveButton_Click;

            var actionRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 0)
            };
            actionRow.Controls.Add(saveButton);

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(actionRow, 0, 2);
            layout.Controls.Add(_validationLabel, 0, 3);
            card.Controls.Add(layout);
            return card;
        }

        private Control BuildFlightCard()
        {
            var card = new CardPanel { Dock = DockStyle.Top, Height = 176 };
            var layout = CreateFormCard("Flight", "Telemetry source and safe service behavior.");

            _telemetryUrlTextBox = CreateTextBox(560);
            _dryRunCheckBox = CreateCheckBox("Keep GSX calls in dry-run mode");

            AddField(layout, 2, "Telemetry URL", _telemetryUrlTextBox);
            AddField(layout, 3, "Safety mode", _dryRunCheckBox);
            card.Controls.Add(layout);
            return card;
        }

        private Control BuildRecognitionCard()
        {
            var card = new CardPanel { Dock = DockStyle.Top, Height = 218 };
            var layout = CreateFormCard("Recognition", "Local phrase input, confidence, and mic sensitivity.");

            _inputDeviceComboBox = CreateComboBox(560);
            _minConfidenceUpDown = CreateNumeric(0m, 1m, 0.01m, 2, 140);
            _inputSensitivityUpDown = CreateNumeric(0m, 1m, 0.01m, 2, 140);

            AddField(layout, 2, "Microphone", _inputDeviceComboBox);
            AddField(layout, 3, "Speech confidence", _minConfidenceUpDown);
            AddField(layout, 4, "Input sensitivity", _inputSensitivityUpDown);
            card.Controls.Add(layout);
            return card;
        }

        private Control BuildAudioVoiceCard()
        {
            var card = new CardPanel { Dock = DockStyle.Top, Height = 362 };
            var layout = CreateFormCard("Audio + Voice", "Output routing, OpenAI playback, and cockpit radio shaping.");

            _outputDeviceComboBox = CreateComboBox(560);
            _outputVolumeUpDown = CreateNumeric(0m, 2m, 0.05m, 2, 140);
            _outputChannelComboBox = CreateComboBox(220);
            _outputPanUpDown = CreateNumeric(-1m, 1m, 0.05m, 2, 140);
            _openAiVoiceEnabledCheckBox = CreateCheckBox("Enable OpenAI voice playback");
            _openAiModelComboBox = CreateComboBox(280, "gpt-4o-mini-tts", "tts-1", "tts-1-hd");
            _openAiVoiceComboBox = CreateComboBox(280, "marin", "cedar", "coral", "alloy", "ash", "ballad", "echo", "fable", "nova", "onyx", "sage", "shimmer", "verse");
            _apiKeyTextBox = CreateTextBox(560);
            _apiKeyTextBox.UseSystemPasswordChar = true;

            AddField(layout, 2, "Speaker", _outputDeviceComboBox);
            AddField(layout, 3, "Output volume", _outputVolumeUpDown);
            AddField(layout, 4, "Ramp channel", _outputChannelComboBox);
            AddField(layout, 5, "Output pan", _outputPanUpDown);
            AddField(layout, 6, "Voice playback", _openAiVoiceEnabledCheckBox);
            AddField(layout, 7, "Model", _openAiModelComboBox);
            AddField(layout, 8, "Voice", _openAiVoiceComboBox);
            AddField(layout, 9, "API key", _apiKeyTextBox);

            card.Controls.Add(layout);
            return card;
        }

        private Control BuildAdvancedCard()
        {
            var card = new CardPanel { Dock = DockStyle.Top, Height = 660, Margin = new Padding(0) };
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = UiTheme.CardBackground
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 208));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var title = new Label
            {
                AutoSize = true,
                Text = "Advanced",
                Font = UiTheme.TitleFont(15f),
                ForeColor = UiTheme.TextPrimary
            };

            var subtitle = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(860, 0),
                Margin = new Padding(0, 6, 0, 18),
                Text = "Phrase diagnostics, parser tests, and the live operations console.",
                Font = UiTheme.BodyFont(9.75f),
                ForeColor = UiTheme.TextMuted
            };

            root.Controls.Add(title, 0, 0);
            root.Controls.Add(subtitle, 0, 1);
            root.Controls.Add(BuildDiagnosticsHost(), 0, 2);
            root.Controls.Add(BuildConsoleHost(), 0, 3);

            card.Controls.Add(root);
            return card;
        }

        private Control BuildDiagnosticsHost()
        {
            var host = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = UiTheme.CardBackground
            };
            host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var label = new Label
            {
                AutoSize = true,
                Text = "Phrase Diagnostics",
                Font = UiTheme.BodyFont(10f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary
            };

            _diagnosticPhraseTextBox = CreateTextBox(640);

            var analyzeButton = new Button
            {
                Text = "Analyze Phrase",
                Width = 136,
                Height = 34
            };
            UiTheme.StylePrimaryButton(analyzeButton);
            analyzeButton.Click += delegate
            {
                var handler = AnalyzePhraseRequested;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            };

            var parserTestsButton = new Button
            {
                Text = "Run Parser Tests",
                Width = 146,
                Height = 34
            };
            UiTheme.StyleSecondaryButton(parserTestsButton);
            parserTestsButton.Click += delegate
            {
                var handler = RunParserTestsRequested;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            };

            var buttonRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            buttonRow.Controls.Add(analyzeButton);
            buttonRow.Controls.Add(parserTestsButton);

            _diagnosticResultTextBox = CreateReadoutBox();

            host.Controls.Add(label, 0, 0);
            host.Controls.Add(_diagnosticPhraseTextBox, 0, 1);
            host.Controls.Add(buttonRow, 0, 2);
            host.Controls.Add(_diagnosticResultTextBox, 0, 3);
            return host;
        }

        private Control BuildConsoleHost()
        {
            var host = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = UiTheme.CardBackground
            };
            host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var label = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 14, 0, 10),
                Text = "Operations Console",
                Font = UiTheme.BodyFont(10f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary
            };

            _logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                BackColor = UiTheme.InputBackground,
                ForeColor = UiTheme.TextPrimary,
                Font = new Font("Consolas", 9.25f, FontStyle.Regular, GraphicsUnit.Point)
            };

            host.Controls.Add(label, 0, 0);
            host.Controls.Add(_logBox, 0, 1);
            return host;
        }

        private TableLayoutPanel CreateFormCard(string title, string subtitle)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = UiTheme.CardBackground
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
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
                Font = UiTheme.BodyFont(9.75f),
                ForeColor = UiTheme.TextMuted
            };

            layout.SetColumnSpan(titleLabel, 2);
            layout.SetColumnSpan(subtitleLabel, 2);
            layout.Controls.Add(titleLabel, 0, 0);
            layout.Controls.Add(subtitleLabel, 0, 1);
            return layout;
        }

        private void AddField(TableLayoutPanel layout, int row, string label, Control editor)
        {
            if (layout.RowCount <= row)
            {
                layout.RowCount = row + 1;
            }

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var labelControl = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 9, 0, 0),
                Text = label,
                Font = UiTheme.BodyFont(9.75f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary
            };

            editor.Margin = new Padding(0, 4, 0, 8);

            layout.Controls.Add(labelControl, 0, row);
            layout.Controls.Add(editor, 1, row);
        }

        private TextBox CreateTextBox(int width)
        {
            var textBox = new TextBox
            {
                Width = width,
                BorderStyle = BorderStyle.FixedSingle
            };
            UiTheme.StyleInput(textBox);
            return textBox;
        }

        private NumericUpDown CreateNumeric(decimal min, decimal max, decimal increment, int decimals, int width)
        {
            var numeric = new NumericUpDown
            {
                Width = width,
                Minimum = min,
                Maximum = max,
                Increment = increment,
                DecimalPlaces = decimals,
                BorderStyle = BorderStyle.FixedSingle
            };
            UiTheme.StyleInput(numeric);
            return numeric;
        }

        private ComboBox CreateComboBox(int width, params string[] items)
        {
            var comboBox = new ComboBox
            {
                Width = width,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            comboBox.Items.AddRange(items ?? new string[0]);
            UiTheme.StyleInput(comboBox);
            return comboBox;
        }

        private CheckBox CreateCheckBox(string text)
        {
            return new CheckBox
            {
                AutoSize = true,
                Text = text,
                Font = UiTheme.BodyFont(9.75f),
                ForeColor = UiTheme.TextPrimary
            };
        }

        private RichTextBox CreateReadoutBox()
        {
            return new RichTextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                BackColor = UiTheme.InputBackground,
                ForeColor = UiTheme.TextPrimary,
                Font = UiTheme.BodyFont(9.5f)
            };
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            var settings = AppSettings.CreateDefault();
            settings.TelemetryUrl = (_telemetryUrlTextBox.Text ?? string.Empty).Trim();
            settings.MinConfidence = (double)_minConfidenceUpDown.Value;
            settings.InputSensitivityGate = (double)_inputSensitivityUpDown.Value;
            settings.OutputVolume = (double)_outputVolumeUpDown.Value;
            settings.OutputPan = (double)_outputPanUpDown.Value;
            settings.DryRun = _dryRunCheckBox.Checked;
            settings.OpenAiVoiceEnabled = _openAiVoiceEnabledCheckBox.Checked;
            settings.OpenAiModel = (_openAiModelComboBox.Text ?? string.Empty).Trim();
            settings.OpenAiVoice = (_openAiVoiceComboBox.Text ?? string.Empty).Trim();

            AudioOutputChannel outputChannel;
            if (!Enum.TryParse(_outputChannelComboBox.Text, true, out outputChannel))
            {
                outputChannel = AudioOutputChannel.Both;
            }

            settings.OutputChannel = outputChannel;
            settings.MicrophoneDeviceName = (_inputDeviceComboBox.Text ?? string.Empty).Trim();
            settings.SpeakerDeviceName = (_outputDeviceComboBox.Text ?? string.Empty).Trim();

            var outputDevice = _outputDevices.FirstOrDefault(device => string.Equals(device.Name, settings.SpeakerDeviceName, StringComparison.OrdinalIgnoreCase));
            settings.SpeakerDeviceId = outputDevice == null ? null : outputDevice.Id;

            var validation = settings.Validate();
            if (!string.IsNullOrWhiteSpace(validation))
            {
                _validationLabel.Text = validation;
                return;
            }

            PendingSettings = settings;
            PendingApiKey = (_apiKeyTextBox.Text ?? string.Empty).Trim();
            _validationLabel.Text = "Settings ready. Applying now.";

            var handler = SaveRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private static void FillCombo(ComboBox comboBox, IEnumerable<string> items)
        {
            comboBox.Items.Clear();
            comboBox.Items.AddRange((items ?? Enumerable.Empty<string>()).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
        }

        private static decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
