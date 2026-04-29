using System;

namespace SimpleOps.GsxRamp
{
    internal sealed class Options
    {
        public string TelemetryUrl = "http://127.0.0.1:4789/telemetry";
        public double MinConfidence = 0.72d;
        public bool NoSpeech;
        public bool NoVoiceFeedback;
        public bool DryRun;
        public int RunDurationSeconds;
        public string TestPhrase;
        public bool RunParserTests;

        public static Options Parse(string[] args)
        {
            var options = new Options();

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i] ?? string.Empty;
                switch (arg.ToLowerInvariant())
                {
                    case "--telemetry-url":
                        if (i + 1 >= args.Length) throw new ArgumentException("Missing value for --telemetry-url");
                        options.TelemetryUrl = args[++i];
                        break;
                    case "--min-confidence":
                        if (i + 1 >= args.Length) throw new ArgumentException("Missing value for --min-confidence");
                        options.MinConfidence = double.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
                        break;
                    case "--no-speech":
                        options.NoSpeech = true;
                        break;
                    case "--no-voice":
                        options.NoVoiceFeedback = true;
                        break;
                    case "--dry-run":
                        options.DryRun = true;
                        break;
                    case "--run-duration-seconds":
                        if (i + 1 >= args.Length) throw new ArgumentException("Missing value for --run-duration-seconds");
                        options.RunDurationSeconds = int.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
                        break;
                    case "--test-phrase":
                        if (i + 1 >= args.Length) throw new ArgumentException("Missing value for --test-phrase");
                        options.TestPhrase = args[++i];
                        break;
                    case "--run-parser-tests":
                        options.RunParserTests = true;
                        break;
                    case "--help":
                    case "-h":
                    case "/?":
                        throw new ArgumentException("help");
                    default:
                        throw new ArgumentException("Unknown argument: " + arg);
                }
            }

            return options;
        }

        public static void PrintUsage()
        {
            Console.WriteLine("SimpleOps.GsxRamp");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  --telemetry-url <url>         Telemetry endpoint. Default http://127.0.0.1:4789/telemetry");
            Console.WriteLine("  --min-confidence <0-1>        Speech confidence threshold. Default 0.72");
            Console.WriteLine("  --no-speech                   Disable speech recognition.");
            Console.WriteLine("  --no-voice                    Disable spoken status feedback.");
            Console.WriteLine("  --dry-run                     Do not send commands to GSX.");
            Console.WriteLine("  --run-duration-seconds <n>    Exit after n seconds.");
            Console.WriteLine("  --test-phrase <text>          Trigger one phrase immediately.");
            Console.WriteLine("  --run-parser-tests            Run built-in parser and safety tests.");
        }
    }
}
