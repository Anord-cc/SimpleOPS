using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SimpleOps.GsxRamp
{
    internal sealed class SettingsForm : Form
    {
        private readonly IList<AudioInputDeviceInfo> _inputDevices;
        private readonly IList<AudioOutputDeviceInfo> _outputDevices;

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

        public SettingsForm(AppSettings settings, string apiKey, IList<AudioInputDeviceInfo> inputDevices, IList<AudioOutputDeviceInfo> outputDevices)
        {
            _inputDevices = inputDevices ?? new List<AudioInputDeviceInfo>();
            _outputDevices = outputDevices ?? new List<AudioOutputDeviceInfo>();
            UpdatedSettings = (settings ?? AppSettings.CreateDefault()).Clone();
            UpdatedApiKey = apiKey ?? string.Empty;

            Text = "SimpleOps Settings";
            Width = 920;
            Height = 780;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UiTheme.WindowBackground;
            Font = UiTheme.BodyFont(9.75f);

            BuildUi();
            LoadSettings();
        }

        public AppSettings UpdatedSettings { get; private set; }

        public string UpdatedApiKey { get; private set; }

        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = UiTheme.WindowBackground,
                Padding = new Padding(18, 16, 18, 18)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));

            root.Controls.Add(BuildHeroPanel(), 0, 0);
            root.Controls.Add(BuildContentPanel(), 0, 1);
            root.Controls.Add(BuildFooterPanel(), 0, 2);

            Controls.Add(root);
        }

        private Control BuildHeroPanel()
        {
            var hero = new GradientPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 18, 24, 18),
                Margin = new Padding(0, 0, 0, 16)
            };

            var title = new Label
            {
                AutoSize = true,
                Text = "Settings",
                Font = UiTheme.TitleFont(24f),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            var subtitle = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(700, 0),
                Text = "Tune telemetry, radio behavior, audio routing, and OpenAI voice without leaving the command deck.",
                Font = UiTheme.BodyFont(10.25f),
                ForeColor = Color.FromArgb(231, 242, 244),
                BackColor = Color.Transparent
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(subtitle, 0, 1);
            hero.Controls.Add(layout);
            return hero;
        }

        private Control BuildContentPanel()
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
                RowCount = 4,
                Width = 844,
                BackColor = UiTheme.WindowBackground
            };

            stack.Controls.Add(BuildFlightCard(), 0, 0);
            stack.Controls.Add(BuildRecognitionCard(), 0, 1);
            stack.Controls.Add(BuildAudioCard(), 0, 2);
            stack.Controls.Add(BuildVoiceCard(), 0, 3);

            scroll.Controls.Add(stack);
            return scroll;
        }

        private Control BuildFlightCard()
        {
            var card = new CardPanel { Dock = DockStyle.Top, Height = 170 };
            var form = CreateFormGrid("Flight Link", "Where telemetry comes from and how safely the desktop app behaves during service testing.");

            _telemetryUrlTextBox = CreateTextBox();
            _dryRunCheckBox = CreateCheckBox("Keep GSX calls in dry-run mode");

            AddField(form, 2, "Telemetry URL", _telemetryUrlTextBox);
            AddField(form, 3, "Service safety", _dryRunCheckBox);

            card.Controls.Add(form);
            return card;
        }

        private Control BuildRecognitionCard()
        {
            var card = new CardPanel { Dock = DockStyle.Top, Height = 220 };
            var form = CreateFormGrid("Recognition", "Choose the cockpit mic path and tune how strict the local phrase recognizer should be.");

            _inputDeviceComboBox = CreateComboBox(_inputDevices.Select(device => device.Name).ToArray());
            _minConfidenceUpDown = CreateNumeric(0m, 1m, 0.01m, 2);
            _inputSensitivityUpDown = CreateNumeric(0m, 1m, 0.01m, 2);

            AddField(form, 2, "Microphone", _inputDeviceComboBox);
            AddField(form, 3, "Speech confidence", _minConfidenceUpDown);
            AddField(form, 4, "Input sensitivity", _inputSensitivityUpDown);

            card.Controls.Add(form);
            return card;
        }

        private Control BuildAudioCard()
        {
            var card = new CardPanel { Dock = DockStyle.Top, Height = 250 };
            var form = CreateFormGrid("Audio Routing", "Shape the radio output so the ramp voice lands where you expect in the cockpit audio field.");

            _outputDeviceComboBox = CreateComboBox(_outputDevices.Select(device => device.Name).ToArray());
            _outputVolumeUpDown = CreateNumeric(0m, 2m, 0.05m, 2);
            _outputChannelComboBox = CreateComboBox(Enum.GetNames(typeof(AudioOutputChannel)));
            _outputPanUpDown = CreateNumeric(-1m, 1m, 0.05m, 2);

            AddField(form, 2, "Speaker", _outputDeviceComboBox);
            AddField(form, 3, "Output volume", _outputVolumeUpDown);
            AddField(form, 4, "Ramp channel", _outputChannelComboBox);
            AddField(form, 5, "Output pan", _outputPanUpDown);

            card.Controls.Add(form);
            return card;
        }

        private Control BuildVoiceCard()
        {
            var card = new CardPanel { Dock = DockStyle.Top, Height = 280, Margin = new Padding(0) };
            var form = CreateFormGrid("OpenAI Voice", "Keep local recognition for control, then use OpenAI TTS for the ramp voice that talks back to you.");

            _openAiVoiceEnabledCheckBox = CreateCheckBox("Enable OpenAI voice playback");
            _openAiModelComboBox = CreateComboBox("gpt-4o-mini-tts", "tts-1", "tts-1-hd");
            _openAiVoiceComboBox = CreateComboBox("marin", "cedar", "coral", "alloy", "ash", "ballad", "echo", "fable", "nova", "onyx", "sage", "shimmer", "verse");
            _apiKeyTextBox = CreateTextBox();
            _apiKeyTextBox.UseSystemPasswordChar = true;

            AddField(form, 2, "Voice engine", _openAiVoiceEnabledCheckBox);
            AddField(form, 3, "Model", _openAiModelComboBox);
            AddField(form, 4, "Voice", _openAiVoiceComboBox);
            AddField(form, 5, "API key", _apiKeyTextBox);

            card.Controls.Add(form);
            return card;
        }

        private Control BuildFooterPanel()
        {
            var footer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = UiTheme.WindowBackground
            };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _validationLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = UiTheme.BodyFont(9.5f, FontStyle.Bold),
                ForeColor = UiTheme.Danger
            };

            var actions = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Right,
                BackColor = UiTheme.WindowBackground,
                Padding = new Padding(0, 14, 0, 0)
            };

            var saveButton = new Button { Text = "Save Changes", Width = 138, Height = 38 };
            var cancelButton = new Button { Text = "Cancel", Width = 110, Height = 38 };
            UiTheme.StylePrimaryButton(saveButton);
            UiTheme.StyleSecondaryButton(cancelButton);

            saveButton.Click += SaveButton_Click;
            cancelButton.Click += delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            actions.Controls.Add(saveButton);
            actions.Controls.Add(cancelButton);

            footer.Controls.Add(_validationLabel, 0, 0);
            footer.Controls.Add(actions, 1, 0);
            return footer;
        }

        private TableLayoutPanel CreateFormGrid(string title, string subtitle)
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = UiTheme.CardBackground
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

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
                MaximumSize = new Size(590, 0),
                Margin = new Padding(0, 6, 0, 18),
                Text = subtitle,
                Font = UiTheme.BodyFont(9.75f),
                ForeColor = UiTheme.TextMuted
            };

            grid.SetColumnSpan(titleLabel, 2);
            grid.SetColumnSpan(subtitleLabel, 2);
            grid.Controls.Add(titleLabel, 0, 0);
            grid.Controls.Add(subtitleLabel, 0, 1);
            return grid;
        }

        private void AddField(TableLayoutPanel grid, int row, string label, Control editor)
        {
            if (grid.RowCount <= row)
            {
                grid.RowCount = row + 1;
            }

            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var labelControl = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 0),
                Text = label,
                Font = UiTheme.BodyFont(9.75f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary
            };

            editor.Margin = new Padding(0, 4, 0, 10);

            grid.Controls.Add(labelControl, 0, row);
            grid.Controls.Add(editor, 1, row);
        }

        private TextBox CreateTextBox()
        {
            var textBox = new TextBox
            {
                Width = 420,
                Height = 32,
                BorderStyle = BorderStyle.FixedSingle
            };
            UiTheme.StyleInput(textBox);
            return textBox;
        }

        private NumericUpDown CreateNumeric(decimal min, decimal max, decimal increment, int decimals)
        {
            var numeric = new NumericUpDown
            {
                Width = 140,
                Minimum = min,
                Maximum = max,
                Increment = increment,
                DecimalPlaces = decimals,
                BorderStyle = BorderStyle.FixedSingle
            };
            UiTheme.StyleInput(numeric);
            return numeric;
        }

        private ComboBox CreateComboBox(params string[] items)
        {
            var comboBox = new ComboBox
            {
                Width = 420,
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

        private void LoadSettings()
        {
            _telemetryUrlTextBox.Text = UpdatedSettings.TelemetryUrl ?? string.Empty;
            _minConfidenceUpDown.Value = ClampDecimal((decimal)UpdatedSettings.MinConfidence, _minConfidenceUpDown.Minimum, _minConfidenceUpDown.Maximum);
            _inputSensitivityUpDown.Value = ClampDecimal((decimal)UpdatedSettings.InputSensitivityGate, _inputSensitivityUpDown.Minimum, _inputSensitivityUpDown.Maximum);
            _outputVolumeUpDown.Value = ClampDecimal((decimal)UpdatedSettings.OutputVolume, _outputVolumeUpDown.Minimum, _outputVolumeUpDown.Maximum);
            _outputPanUpDown.Value = ClampDecimal((decimal)UpdatedSettings.OutputPan, _outputPanUpDown.Minimum, _outputPanUpDown.Maximum);
            _dryRunCheckBox.Checked = UpdatedSettings.DryRun;
            _openAiVoiceEnabledCheckBox.Checked = UpdatedSettings.OpenAiVoiceEnabled;
            _openAiModelComboBox.Text = UpdatedSettings.OpenAiModel ?? "gpt-4o-mini-tts";
            _openAiVoiceComboBox.Text = UpdatedSettings.OpenAiVoice ?? "marin";
            _outputChannelComboBox.Text = UpdatedSettings.OutputChannel.ToString();
            _apiKeyTextBox.Text = UpdatedApiKey;

            if (!string.IsNullOrWhiteSpace(UpdatedSettings.MicrophoneDeviceName))
            {
                _inputDeviceComboBox.Text = UpdatedSettings.MicrophoneDeviceName;
            }

            if (!string.IsNullOrWhiteSpace(UpdatedSettings.SpeakerDeviceName))
            {
                _outputDeviceComboBox.Text = UpdatedSettings.SpeakerDeviceName;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            var settings = UpdatedSettings.Clone();
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

            UpdatedSettings = settings;
            UpdatedApiKey = (_apiKeyTextBox.Text ?? string.Empty).Trim();
            DialogResult = DialogResult.OK;
            Close();
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
