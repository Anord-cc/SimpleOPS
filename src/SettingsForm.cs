// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
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

        private Panel _scrollHost;
        private TableLayoutPanel _stackHost;
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
            SelectComboValue(_openAiModelComboBox, PendingSettings.OpenAiModel ?? "gpt-4o-mini-tts");
            SelectComboValue(_openAiVoiceComboBox, PendingSettings.OpenAiVoice ?? "marin");
            SelectComboValue(_outputChannelComboBox, PendingSettings.OutputChannel.ToString());
            _apiKeyTextBox.Text = PendingApiKey;
            SelectComboValue(_inputDeviceComboBox, PendingSettings.MicrophoneDeviceName);
            SelectComboValue(_outputDeviceComboBox, PendingSettings.SpeakerDeviceName);
            _validationLabel.Text = "Ready.";
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
            SuspendLayout();

            _scrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = UiTheme.WindowBackground,
                Padding = new Padding(0, 0, 10, 0)
            };

            _stackHost = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 4,
                BackColor = UiTheme.WindowBackground,
                Margin = new Padding(0)
            };
            _stackHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            _stackHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            var saveCard = BuildSaveCard();
            var flightCard = BuildFlightCard();
            var recognitionCard = BuildRecognitionCard();
            var audioCard = BuildAudioVoiceCard();
            var advancedCard = BuildAdvancedCard();

            _stackHost.Controls.Add(saveCard, 0, 0);
            _stackHost.Controls.Add(flightCard, 0, 1);
            _stackHost.Controls.Add(recognitionCard, 1, 1);
            _stackHost.Controls.Add(audioCard, 0, 2);
            _stackHost.Controls.Add(advancedCard, 0, 3);
            _stackHost.SetColumnSpan(saveCard, 2);
            _stackHost.SetColumnSpan(audioCard, 2);
            _stackHost.SetColumnSpan(advancedCard, 2);

            _scrollHost.Controls.Add(_stackHost);
            Controls.Add(_scrollHost);

            Resize += delegate { LayoutContentWidth(); };
            LayoutContentWidth();
            ResumeLayout();
        }

        private Control BuildSaveCard()
        {
            var card = CreateAutoCard();
            var layout = CreateSectionLayout("Apply Changes", "Save settings in place and keep working in the same window.");

            var saveButton = new Button
            {
                Text = "Save Changes",
                Width = 152,
                Height = 38
            };
            UiTheme.StylePrimaryButton(saveButton);
            saveButton.Click += SaveButton_Click;

            _validationLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(700, 0),
                Margin = new Padding(12, 9, 0, 0),
                ForeColor = UiTheme.TextMuted,
                Font = UiTheme.BodyFont(9.5f, FontStyle.Bold),
                Text = "Ready."
            };

            var actionRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 4, 0, 0),
                BackColor = Color.Transparent
            };
            actionRow.Controls.Add(saveButton);
            actionRow.Controls.Add(_validationLabel);

            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(actionRow, 0, 2);
            layout.SetColumnSpan(actionRow, 2);
            card.Controls.Add(layout);
            return card;
        }

        private Control BuildFlightCard()
        {
            var card = CreateAutoCard();
            var layout = CreateSectionLayout("Flight", "Telemetry source and service safety.");

            _telemetryUrlTextBox = CreateStretchTextBox();
            _dryRunCheckBox = CreateCheckBox("Keep GSX calls in dry-run mode");

            AddField(layout, 2, "Telemetry URL", _telemetryUrlTextBox);
            AddField(layout, 3, "Safety mode", _dryRunCheckBox);

            card.Controls.Add(layout);
            return card;
        }

        private Control BuildRecognitionCard()
        {
            var card = CreateAutoCard();
            var layout = CreateSectionLayout("Recognition", "Microphone routing, confidence, and sensitivity.");

            _inputDeviceComboBox = CreateStretchComboBox();
            _minConfidenceUpDown = CreateNumeric(0m, 1m, 0.01m, 2);
            _inputSensitivityUpDown = CreateNumeric(0m, 1m, 0.01m, 2);

            AddField(layout, 2, "Microphone", _inputDeviceComboBox);
            AddField(layout, 3, "Speech confidence", _minConfidenceUpDown);
            AddField(layout, 4, "Input sensitivity", _inputSensitivityUpDown);

            card.Controls.Add(layout);
            return card;
        }

        private Control BuildAudioVoiceCard()
        {
            var card = CreateAutoCard();
            var layout = CreateSectionLayout("Audio + Voice", "Speaker routing, radio shaping, and OpenAI playback.");

            _outputDeviceComboBox = CreateStretchComboBox();
            _outputVolumeUpDown = CreateNumeric(0m, 2m, 0.05m, 2);
            _outputChannelComboBox = CreateListComboBox(Enum.GetNames(typeof(AudioOutputChannel)));
            _outputPanUpDown = CreateNumeric(-1m, 1m, 0.05m, 2);
            _openAiVoiceEnabledCheckBox = CreateCheckBox("Enable OpenAI voice playback");
            _openAiModelComboBox = CreateListComboBox("gpt-4o-mini-tts", "tts-1", "tts-1-hd");
            _openAiVoiceComboBox = CreateListComboBox("marin", "cedar", "coral", "alloy", "ash", "ballad", "echo", "fable", "nova", "onyx", "sage", "shimmer", "verse");
            _apiKeyTextBox = CreateStretchTextBox();
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
            var card = CreateAutoCard();
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = UiTheme.CardBackground,
                Margin = new Padding(0)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

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
                MaximumSize = new Size(920, 0),
                Margin = new Padding(0, 6, 0, 16),
                Text = "Phrase diagnostics, parser tests, and the live operations console.",
                Font = UiTheme.BodyFont(9.75f),
                ForeColor = UiTheme.TextMuted
            };

            root.Controls.Add(title, 0, 0);
            root.Controls.Add(subtitle, 0, 1);
            root.Controls.Add(BuildAdvancedWorkspace(), 0, 2);

            card.Controls.Add(root);
            return card;
        }

        private Control BuildAdvancedWorkspace()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = UiTheme.CardBackground,
                Margin = new Padding(0)
            };
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            grid.Controls.Add(BuildDiagnosticsCard(), 0, 0);
            grid.Controls.Add(BuildConsoleCard(), 0, 1);
            return grid;
        }

        private Control BuildDiagnosticsCard()
        {
            var card = CreateSubCard();
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = UiTheme.RaisedCardBackground,
                Margin = new Padding(0)
            };

            var title = new Label
            {
                AutoSize = true,
                Text = "Phrase Diagnostics",
                Font = UiTheme.BodyFont(10f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary
            };

            var subtitle = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(900, 0),
                Margin = new Padding(0, 6, 0, 12),
                Text = "Check how a phrase is normalized, classified, and gated before GSX is touched.",
                Font = UiTheme.BodyFont(9.5f),
                ForeColor = UiTheme.TextMuted
            };

            _diagnosticPhraseTextBox = CreateStretchTextBox();
            _diagnosticPhraseTextBox.Margin = new Padding(0, 0, 0, 10);

            var analyzeButton = new Button
            {
                Text = "Analyze Phrase",
                Width = 138,
                Height = 36
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
                Width = 148,
                Height = 36
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

            var actionRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 10),
                BackColor = Color.Transparent
            };
            actionRow.Controls.Add(analyzeButton);
            actionRow.Controls.Add(parserTestsButton);

            _diagnosticResultTextBox = CreateReadoutBox();
            _diagnosticResultTextBox.Height = 120;

            root.Controls.Add(title, 0, 0);
            root.Controls.Add(subtitle, 0, 1);
            root.Controls.Add(_diagnosticPhraseTextBox, 0, 2);
            root.Controls.Add(actionRow, 0, 3);
            root.Controls.Add(_diagnosticResultTextBox, 0, 4);
            card.Controls.Add(root);
            return card;
        }

        private Control BuildConsoleCard()
        {
            var card = CreateSubCard();
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = UiTheme.RaisedCardBackground,
                Margin = new Padding(0)
            };

            var title = new Label
            {
                AutoSize = true,
                Text = "Operations Console",
                Font = UiTheme.BodyFont(10f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary
            };

            var subtitle = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(900, 0),
                Margin = new Padding(0, 6, 0, 12),
                Text = "Live application logs, parser output, and runtime guardrails.",
                Font = UiTheme.BodyFont(9.5f),
                ForeColor = UiTheme.TextMuted
            };

            _logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                Height = 260,
                BackColor = UiTheme.InputBackground,
                ForeColor = UiTheme.TextPrimary,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Font = new Font("Consolas", 9.25f, FontStyle.Regular, GraphicsUnit.Point)
            };

            root.Controls.Add(title, 0, 0);
            root.Controls.Add(subtitle, 0, 1);
            root.Controls.Add(_logBox, 0, 2);
            card.Controls.Add(root);
            return card;
        }

        private TableLayoutPanel CreateSectionLayout(string title, string subtitle)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = UiTheme.CardBackground,
                Margin = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 166));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var titleLabel = new Label
            {
                AutoSize = true,
                Text = title,
                Font = UiTheme.TitleFont(14.5f),
                ForeColor = UiTheme.TextPrimary
            };

            var subtitleLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(900, 0),
                Margin = new Padding(0, 6, 0, 14),
                Text = subtitle,
                Font = UiTheme.BodyFont(9.5f),
                ForeColor = UiTheme.TextMuted
            };

            layout.Controls.Add(titleLabel, 0, 0);
            layout.Controls.Add(subtitleLabel, 0, 1);
            layout.SetColumnSpan(titleLabel, 2);
            layout.SetColumnSpan(subtitleLabel, 2);
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
                Margin = new Padding(0, 10, 0, 0),
                Text = label,
                Font = UiTheme.BodyFont(9.75f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary
            };

            editor.Margin = new Padding(0, 4, 0, 10);
            if (!(editor is NumericUpDown) && !(editor is CheckBox))
            {
                editor.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            }

            layout.Controls.Add(labelControl, 0, row);
            layout.Controls.Add(editor, 1, row);
        }

        private CardPanel CreateAutoCard()
        {
            return new CardPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top
            };
        }

        private CardPanel CreateSubCard()
        {
            return new CardPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                BackColor = UiTheme.RaisedCardBackground
            };
        }

        private TextBox CreateStretchTextBox()
        {
            var textBox = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Width = 420
            };
            UiTheme.StyleInput(textBox);
            return textBox;
        }

        private NumericUpDown CreateNumeric(decimal min, decimal max, decimal increment, int decimals)
        {
            var numeric = new NumericUpDown
            {
                Width = 150,
                Minimum = min,
                Maximum = max,
                Increment = increment,
                DecimalPlaces = decimals,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Left
            };
            UiTheme.StyleInput(numeric);
            return numeric;
        }

        private ComboBox CreateStretchComboBox()
        {
            var comboBox = new ComboBox
            {
                Width = 420,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            UiTheme.StyleInput(comboBox);
            return comboBox;
        }

        private ComboBox CreateListComboBox(params string[] items)
        {
            var comboBox = CreateStretchComboBox();
            comboBox.Items.AddRange(items ?? new string[0]);
            if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
            }

            return comboBox;
        }

        private CheckBox CreateCheckBox(string text)
        {
            return new CheckBox
            {
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 8),
                Text = text,
                Font = UiTheme.BodyFont(9.75f),
                ForeColor = UiTheme.TextPrimary,
                BackColor = Color.Transparent
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
                ScrollBars = RichTextBoxScrollBars.Vertical,
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
                _validationLabel.ForeColor = Color.White;
                _validationLabel.Text = validation;
                return;
            }

            PendingSettings = settings;
            PendingApiKey = (_apiKeyTextBox.Text ?? string.Empty).Trim();
            _validationLabel.ForeColor = UiTheme.TextPrimary;
            _validationLabel.Text = "Settings saved. Reloading services.";

            var handler = SaveRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void LayoutContentWidth()
        {
            if (_scrollHost == null || _stackHost == null)
            {
                return;
            }

            var width = Math.Max(920, _scrollHost.ClientSize.Width - 18);
            _stackHost.Width = width;
        }

        private static void FillCombo(ComboBox comboBox, IEnumerable<string> items)
        {
            comboBox.Items.Clear();
            comboBox.Items.AddRange((items ?? Enumerable.Empty<string>()).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
        }

        private static void SelectComboValue(ComboBox comboBox, string value)
        {
            if (comboBox == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                for (int index = 0; index < comboBox.Items.Count; index++)
                {
                    var item = comboBox.Items[index] as string;
                    if (string.Equals(item, value, StringComparison.OrdinalIgnoreCase))
                    {
                        comboBox.SelectedIndex = index;
                        return;
                    }
                }
            }

            if (comboBox.Items.Count > 0 && comboBox.SelectedIndex < 0)
            {
                comboBox.SelectedIndex = 0;
            }
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
