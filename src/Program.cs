using System;

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
                    Options.PrintUsage();
                    return 0;
                }

                Console.Error.WriteLine(ex.Message);
                Console.WriteLine();
                Options.PrintUsage();
                return 1;
            }

            try
            {
                if (options.RunParserTests)
                {
                    return ParserTestHarness.Run();
                }

                using (var controller = new RampController(options))
                {
                    controller.Start();
                    controller.RunLoop();
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }
    }
}
