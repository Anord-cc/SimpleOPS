using System;
using System.Windows.Forms;

namespace SimpleOps.GsxRamp
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Options options;
            try
            {
                options = Options.Parse(args);
            }
            catch (ArgumentException ex)
            {
                if (string.Equals(ex.Message, "help", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(
                        "Arguments:" + Environment.NewLine +
                        "--telemetry-url <url>" + Environment.NewLine +
                        "--min-confidence <0-1>" + Environment.NewLine +
                        "--no-speech" + Environment.NewLine +
                        "--no-voice" + Environment.NewLine +
                        "--dry-run" + Environment.NewLine +
                        "--run-duration-seconds <n>" + Environment.NewLine +
                        "--test-phrase <text>" + Environment.NewLine +
                        "--run-parser-tests",
                        "SimpleOps GSX Ramp",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return 0;
                }

                MessageBox.Show(ex.Message, "SimpleOps GSX Ramp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 1;
            }

            try
            {
                if (options.RunParserTests)
                {
                    int result = ParserTestHarness.Run();
                    MessageBox.Show(
                        result == 0 ? "Parser tests passed." : "Parser tests failed. Run from a terminal if you need exit-code visibility.",
                        "SimpleOps GSX Ramp",
                        MessageBoxButtons.OK,
                        result == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                    return result;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new RampControlForm(options));
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "SimpleOps GSX Ramp", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }
    }
}
