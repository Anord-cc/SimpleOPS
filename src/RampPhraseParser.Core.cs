using System;
using System.Collections.Generic;

namespace SimpleOps.GsxRamp
{
    internal sealed partial class RampPhraseParser
    {
        private static readonly HashSet<string> FalsePositivePhrases = new HashSet<string>(StringComparer.Ordinal)
        {
            "ground speed alive",
            "boarding music is loud",
            "fuel pump on",
            "fuel flow stable",
            "engine anti ice on",
            "taxi light on",
            "landing lights on",
            "parking brake test",
            "passengers are complaining",
            "cargo temperature",
            "push to talk",
            "left brake",
            "right brake"
        };

        public RampCommand Parse(string phrase)
        {
            var normalized = TextUtility.NormalizeText(phrase);
            var command = new RampCommand
            {
                RawPhrase = phrase,
                NormalizedPhrase = normalized,
                Type = RampCommandType.Unknown,
                Quality = MatchQuality.None,
                Reason = "No command rule matched."
            };

            if (normalized.Length == 0)
            {
                command.Type = RampCommandType.Ignored;
                command.Quality = MatchQuality.Blocked;
                command.Reason = "Phrase was empty after normalization.";
                return command;
            }

            if (FalsePositivePhrases.Contains(normalized))
            {
                command.Type = RampCommandType.Ignored;
                command.Quality = MatchQuality.Blocked;
                command.Reason = "Phrase matched a false-positive safety blocklist.";
                return command;
            }

            if (TryParseRampContact(command)) return command;
            if (TryParseDeboarding(command)) return command;
            if (TryParseBoarding(command)) return command;
            if (TryParseJetwayAndStairs(command)) return command;
            if (TryParseBaggageAndCargo(command)) return command;
            if (TryParseCatering(command)) return command;
            if (TryParseFuel(command)) return command;
            if (TryParsePushback(command)) return command;
            if (TryParseBrakes(command)) return command;
            if (TryParseEngineStart(command)) return command;
            if (TryParseDeicing(command)) return command;
            if (TryParseGuidance(command)) return command;
            if (TryParseGenericService(command)) return command;

            return command;
        }

        private static void Fill(RampCommand command, RampCommandType type, MatchQuality quality, string reason, params string[] menuPatterns)
        {
            command.Type = type;
            command.Quality = quality;
            command.Reason = reason;
            command.MenuPatterns = menuPatterns ?? new string[0];
        }
    }
}
