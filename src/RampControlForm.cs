using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleOps.GsxRamp
{
    internal sealed class RampControlForm : Form
    {
        private const int WmUserSimConnect = 0x0402;

        private readonly Options _options;

        private Label _statusLabel;
        private Label _detailLabel;
        private Label _configLabel;
        private TextBox _logBox;
        private Button _parserTestsButton;

        private GsxMenuDriver _gsxMenuDriver;
        private RampController _rampController;
        private Timer _autoCloseTimer;

        public RampControlForm(Options options)
        {
            _options = options;

            Text = "SimpleOps GSX Ramp";
            Width = 860;
            Height = 430;
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

            _statusLabel = new Label
            {
                AutoSize = false,
                Left = 20,
                Top = 18,
                Width = 800,
                Height = 24,
                Text = "Starting..."
            };

            _detailLabel = new Label
            {
                AutoSize = false,
                Left = 20,
                Top = 50,
                Width = 800,
                Height = 40,
                Text = "Waiting for telemetry..."
            };

            _configLabel = new Label
            {
                AutoSize = false,
                Left = 20,
                Top = 95,
                Width = 800,
                Height = 40,
                Text = ""
            };

            _logBox = new TextBox
            {
                Left = 20,
                Top = 145,
                Width = 800,
                Height = 200,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true
            };

            _parserTestsButton = new Button
            {
                Left = 20,
                Top = 355,
                Width = 180,
                Height = 30,
                Text = "Run parser tests"
            };
            _parserTestsButton.Click += ParserTestsButton_Click;

            Controls.Add(_statusLabel);
            Controls.Add(_detailLabel);
            Controls.Add(_configLabel);
            Controls.Add(_logBox);
            Controls.Add(_parserTestsButton);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            _configLabel.Text =
                "Telemetry: " + _options.TelemetryUrl + Environment.NewLine +
                "GSX mode: SimConnect Remote Control";

            var paths = GsxPaths.Detect();
            _detailLabel.Text = "GSX panel: " + paths.GsxPanelPath;

            _gsxMenuDriver = new GsxMenuDriver(paths, Handle, WmUserSimConnect, Log);
            _rampController = new RampController(_options, new TelemetryClient(_options.TelemetryUrl), _gsxMenuDriver, new RampPhraseParser(), Log, SetStatus);
            _rampController.Start();

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
            try
            {
                _autoCloseTimer?.Stop();
                _autoCloseTimer?.Dispose();
            }
            catch
            {
            }

            try
            {
                _rampController?.Dispose();
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

            base.OnClosed(e);
        }

        private void ParserTestsButton_Click(object sender, EventArgs e)
        {
            int code = ParserTestHarness.Run();
            MessageBox.Show(
                code == 0 ? "Parser tests passed." : "Parser tests failed. See details in the log output mode.",
                "SimpleOps GSX Ramp",
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

            _statusLabel.Text = message;
        }

        private void Log(string message)
        {
            string line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(Log), message);
                return;
            }

            _logBox.AppendText(line + Environment.NewLine);
        }
    }
}
