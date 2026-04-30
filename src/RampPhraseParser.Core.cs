using System;
using System.Collections.Generic;
using System.Linq;

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

        private readonly IDictionary<string, string[]> _aliases;

        public RampPhraseParser(IDictionary<string, string[]> aliases = null)
        {
            _aliases = aliases ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        }

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

        public string[] GetAllRecognizedPhrases()
        {
            var phrases = new HashSet<string>(BuiltInGrammarPhrases, StringComparer.OrdinalIgnoreCase);
            foreach (var pair in _aliases)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                for (int i = 0; i < pair.Value.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(pair.Value[i]))
                    {
                        phrases.Add(pair.Value[i].Trim());
                    }
                }
            }

            return phrases.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private bool ContainsAny(string text, string aliasKey, params string[] builtIns)
        {
            return TextUtility.ContainsAny(text, GetPhraseVariants(aliasKey, builtIns));
        }

        private string[] GetPhraseVariants(string aliasKey, params string[] builtIns)
        {
            var variants = new List<string>();
            if (builtIns != null)
            {
                variants.AddRange(builtIns);
            }

            string[] aliases;
            if (_aliases != null && !string.IsNullOrWhiteSpace(aliasKey) && _aliases.TryGetValue(aliasKey, out aliases) && aliases != null)
            {
                variants.AddRange(aliases);
            }

            return variants.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static readonly string[] BuiltInGrammarPhrases = new[]
        {
            "ramp cockpit","ramp flight deck","ground crew cockpit","ramp do you read","can we get ramp on headset","ground crew do you read","can we get ground on headset",
            "start boarding","begin boarding","we are ready for boarding","send the passengers","board the passengers","boarding may begin","ready to board passengers",
            "pause boarding","hold boarding","stop boarding","resume boarding","continue boarding","boarding complete","how is boarding going","boarding status",
            "start deboarding","begin deboarding","start deplaning","let the passengers off","begin disembarkation","start passenger deboarding","deboarding complete","hold deboarding","pause deboarding",
            "connect jetway","bring the jetway in","dock the jet bridge","dock the jetway","disconnect jetway","pull the jetway back","remove the jetway","request stairs","bring stairs","send stairs","remove stairs","stairs can be removed",
            "start baggage loading","load the bags","begin loading bags","load baggage","start cargo loading","load cargo","start cargo service","unload bags","start baggage offload","unload baggage","unload cargo","start cargo offload","are the bags loaded","baggage complete","cargo complete",
            "request catering","send catering","bring the catering truck","start catering","remove catering","catering complete","catering can leave",
            "request refueling","send the fuel truck","start refueling","fuel to block fuel","fuel to planned fuel","fuel to eight point two tons","fuel to 8.2 tons","fuel to 8200 kilos","fuel to 8200 kilograms","add 5000 pounds","top us off","stop fueling","disconnect fuel truck","fueling complete",
            "request pushback","ready for push","ready for pushback","call the tug","send the tug","connect the tug","push tail left","tail left please","push facing left","nose right after push","push tail right","tail right please","push facing right","nose left after push","straight pushback","push straight back","no turn required","hold pushback","stop the push","resume pushback","continue push","cancel pushback","disconnect tug","remove towbar",
            "brakes released","parking brake released","brakes are off","parking brake is off","brakes set","parking brake set","brakes are set","confirm brakes set","confirm brakes released",
            "ready to start engines","ready for engine start","starting engine one","starting engine two","starting both engines","engine start complete","both engines stable","engine one is coming up","engine two is coming up",
            "request deicing","we need deicing","send the deice truck","send the deicing truck","start deicing","apply type one","apply type four","type one and type four","deice wings","deice wings and tail","deicing complete","cancel deicing",
            "request follow me","send follow me car","guide us to the stand","guide us to the gate","request marshaller","need a marshaller","marshaller required","marshalling complete",
            "cancel service","cancel current service","stop current service","what is the status","service status","what are we waiting for","hold service","resume service"
        };
    }
}
