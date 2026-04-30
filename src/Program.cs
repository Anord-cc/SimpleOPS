using System;
using System.Windows.Forms;

namespace SimpleOps.GsxRamp
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            var appPaths = AppPaths.Create();
            using (var logger = new AppLogger(appPaths))
            {
                Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
                {
                    logger.Log("UI thread exception: " + e.Exception);
                };
                AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
                {
                    logger.Log("Unhandled exception: " + Convert.ToString(e.ExceptionObject));
                };

                return Run(args, appPaths, logger);
            }
        }

        private static int Run(string[] args, AppPaths appPaths, AppLogger logger)
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
                        "SimpleOps",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return 0;
                }

                logger.Log("Argument error: " + ex.Message);
                MessageBox.Show(ex.Message, "SimpleOps", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 1;
            }

            try
            {
                if (options.RunParserTests)
                {
                    return ParserTestHarness.Run(logger.Log);
                }

                var settingsStore = new JsonSettingsStore(appPaths, logger.Log);
                var credentialStore = new WindowsCredentialStore();
                var phraseAliasStore = new PhraseAliasStore(appPaths, logger.Log);
                var appSettings = options.ApplyTo(settingsStore.Load());

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new RampControlForm(options, appPaths, settingsStore, credentialStore, phraseAliasStore, appSettings, logger));
                return 0;
            }
            catch (Exception ex)
            {
                logger.Log("Startup error: " + ex);
                MessageBox.Show(ex.ToString(), "SimpleOps", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }
    }
}
