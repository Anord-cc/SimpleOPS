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
            Width = 620;
            Height = 560;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            BuildUi();
            LoadSettings();
        }

        public AppSettings UpdatedSettings { get; private set; }

        public string UpdatedApiKey { get; private set; }

        private void BuildUi()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 13,
                Padding = new Padding(16),
                AutoScroll = true
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _telemetryUrlTextBox = AddTextRow(panel, 0, "Telemetry URL");
            _minConfidenceUpDown = AddDecimalRow(panel, 1, "Speech confidence", 0m, 1m, 0.01m, 2);
            _inputDeviceComboBox = AddComboRow(panel, 2, "Microphone", _inputDevices.Select(device => device.Name).ToArray());
            _outputDeviceComboBox = AddComboRow(panel, 3, "Speaker", _outputDevices.Select(device => device.Name).ToArray());
            _inputSensitivityUpDown = AddDecimalRow(panel, 4, "Input sensitivity", 0m, 1m, 0.01m, 2);
            _outputVolumeUpDown = AddDecimalRow(panel, 5, "Output volume", 0m, 2m, 0.05m, 2);
            _outputChannelComboBox = AddComboRow(panel, 6, "Ramp channel", Enum.GetNames(typeof(AudioOutputChannel)));
            _outputPanUpDown = AddDecimalRow(panel, 7, "Output pan", -1m, 1m, 0.05m, 2);
            _dryRunCheckBox = AddCheckRow(panel, 8, "Dry-run mode");
            _openAiVoiceEnabledCheckBox = AddCheckRow(panel, 9, "OpenAI voice");
            _openAiModelComboBox = AddComboRow(panel, 10, "OpenAI model", "gpt-4o-mini-tts", "tts-1", "tts-1-hd");
            _openAiVoiceComboBox = AddComboRow(panel, 11, "OpenAI voice", "marin", "cedar", "coral", "alloy", "ash", "ballad", "echo", "fable", "nova", "onyx", "sage", "shimmer", "verse");
            _apiKeyTextBox = AddTextRow(panel, 12, "OpenAI API key");
            _apiKeyTextBox.UseSystemPasswordChar = true;

            _validationLabel = new Label
            {
                Dock = DockStyle.Top,
                ForeColor = Color.DarkRed,
                Height = 32
            };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 44,
                Padding = new Padding(16, 0, 16, 16)
            };

            var saveButton = new Button { Text = "Save", Width = 100, Height = 28 };
            var cancelButton = new Button { Text = "Cancel", Width = 100, Height = 28 };
            saveButton.Click += SaveButton_Click;
            cancelButton.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };

            buttonPanel.Controls.Add(saveButton);
            buttonPanel.Controls.Add(cancelButton);

            Controls.Add(panel);
            Controls.Add(_validationLabel);
            Controls.Add(buttonPanel);
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

        private static TextBox AddTextRow(TableLayoutPanel panel, int row, string label)
        {
            AddLabel(panel, row, label);
            var textBox = new TextBox { Dock = DockStyle.Top, Width = 320 };
            panel.Controls.Add(textBox, 1, row);
            return textBox;
        }

        private static NumericUpDown AddDecimalRow(TableLayoutPanel panel, int row, string label, decimal min, decimal max, decimal increment, int decimals)
        {
            AddLabel(panel, row, label);
            var control = new NumericUpDown
            {
                Dock = DockStyle.Top,
                Minimum = min,
                Maximum = max,
                Increment = increment,
                DecimalPlaces = decimals,
                Width = 120
            };
            panel.Controls.Add(control, 1, row);
            return control;
        }

        private static ComboBox AddComboRow(TableLayoutPanel panel, int row, string label, params string[] items)
        {
            AddLabel(panel, row, label);
            var comboBox = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 320
            };
            comboBox.Items.AddRange(items ?? new string[0]);
            panel.Controls.Add(comboBox, 1, row);
            return comboBox;
        }

        private static CheckBox AddCheckRow(TableLayoutPanel panel, int row, string label)
        {
            AddLabel(panel, row, label);
            var checkBox = new CheckBox { Dock = DockStyle.Top };
            panel.Controls.Add(checkBox, 1, row);
            return checkBox;
        }

        private static void AddLabel(TableLayoutPanel panel, int row, string text)
        {
            var label = new Label
            {
                Text = text,
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(0, 6, 0, 0)
            };
            panel.Controls.Add(label, 0, row);
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
