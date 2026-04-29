using System;
using System.Collections.Generic;
using System.Speech.Recognition;

namespace SimpleOps.GsxRamp
{
    internal static class ParserTestHarness
    {
        public static int Run()
        {
            int failures = 0;
            var parser = new RampPhraseParser();

            failures += ExpectType(parser, "ramp cockpit", RampCommandType.RampContact);
            failures += ExpectType(parser, "ramp flight deck", RampCommandType.RampContact);
            failures += ExpectType(parser, "ground crew cockpit", RampCommandType.RampContact);
            failures += ExpectType(parser, "ramp do you read", RampCommandType.RampContact);
            failures += ExpectType(parser, "can we get ramp on headset", RampCommandType.RampContact);
            failures += ExpectType(parser, "start boarding", RampCommandType.BoardingStart);
            failures += ExpectType(parser, "begin boarding", RampCommandType.BoardingStart);
            failures += ExpectType(parser, "we are ready for boarding", RampCommandType.BoardingStart);
            failures += ExpectType(parser, "send the passengers", RampCommandType.BoardingStart);
            failures += ExpectType(parser, "board the passengers", RampCommandType.BoardingStart);
            failures += ExpectType(parser, "pause boarding", RampCommandType.BoardingPause);
            failures += ExpectType(parser, "hold boarding", RampCommandType.BoardingPause);
            failures += ExpectType(parser, "resume boarding", RampCommandType.BoardingResume);
            failures += ExpectType(parser, "boarding complete", RampCommandType.BoardingComplete);
            failures += ExpectType(parser, "how is boarding going", RampCommandType.BoardingStatus);
            failures += ExpectType(parser, "start deboarding", RampCommandType.DeboardingStart);
            failures += ExpectType(parser, "start deplaning", RampCommandType.DeboardingStart);
            failures += ExpectType(parser, "let the passengers off", RampCommandType.DeboardingStart);
            failures += ExpectType(parser, "begin disembarkation", RampCommandType.DeboardingStart);
            failures += ExpectType(parser, "deboarding complete", RampCommandType.DeboardingComplete);
            failures += ExpectType(parser, "hold deboarding", RampCommandType.DeboardingHold);
            failures += ExpectType(parser, "connect jetway", RampCommandType.JetwayConnect);
            failures += ExpectType(parser, "bring the jetway in", RampCommandType.JetwayConnect);
            failures += ExpectType(parser, "disconnect jetway", RampCommandType.JetwayDisconnect);
            failures += ExpectType(parser, "request stairs", RampCommandType.StairsRequest);
            failures += ExpectType(parser, "remove stairs", RampCommandType.StairsRemove);
            failures += ExpectType(parser, "start baggage loading", RampCommandType.BaggageLoadStart);
            failures += ExpectType(parser, "load the bags", RampCommandType.BaggageLoadStart);
            failures += ExpectType(parser, "start cargo loading", RampCommandType.CargoLoadStart);
            failures += ExpectType(parser, "unload bags", RampCommandType.BaggageUnloadStart);
            failures += ExpectType(parser, "unload cargo", RampCommandType.CargoUnloadStart);
            failures += ExpectType(parser, "are the bags loaded", RampCommandType.BaggageStatus);
            failures += ExpectType(parser, "request catering", RampCommandType.CateringRequest);
            failures += ExpectType(parser, "remove catering", RampCommandType.CateringRemove);
            failures += ExpectType(parser, "request refueling", RampCommandType.RefuelingRequest);
            failures += ExpectType(parser, "send the fuel truck", RampCommandType.RefuelingRequest);
            failures += ExpectType(parser, "start refueling", RampCommandType.RefuelingRequest);
            failures += ExpectType(parser, "fuel to block fuel", RampCommandType.Unknown);
            failures += ExpectType(parser, "fuel to planned fuel", RampCommandType.Unknown);
            failures += ExpectType(parser, "fuel to eight point two tons", RampCommandType.RefuelingTarget);
            failures += ExpectType(parser, "fuel to 8.2 tons", RampCommandType.RefuelingTarget);
            failures += ExpectType(parser, "fuel to 8200 kilos", RampCommandType.RefuelingTarget);
            failures += ExpectType(parser, "add 5000 pounds", RampCommandType.RefuelingTarget);
            failures += ExpectType(parser, "top us off", RampCommandType.RefuelingRequest);
            failures += ExpectType(parser, "stop fueling", RampCommandType.RefuelingStop);
            failures += ExpectType(parser, "disconnect fuel truck", RampCommandType.RefuelingDisconnect);
            failures += ExpectType(parser, "fueling complete", RampCommandType.RefuelingComplete);
            failures += ExpectType(parser, "request pushback", RampCommandType.PushbackRequest);
            failures += ExpectType(parser, "ready for push", RampCommandType.PushbackRequest);
            failures += ExpectType(parser, "send the tug", RampCommandType.PushbackRequest);
            failures += ExpectType(parser, "push tail left", RampCommandType.PushbackDirection);
            failures += ExpectDirection(parser, "tail left please", PushbackDirection.TailLeftNoseRight);
            failures += ExpectDirection(parser, "nose right after push", PushbackDirection.TailLeftNoseRight);
            failures += ExpectDirection(parser, "push tail right", PushbackDirection.TailRightNoseLeft);
            failures += ExpectDirection(parser, "nose left after push", PushbackDirection.TailRightNoseLeft);
            failures += ExpectDirection(parser, "straight pushback", PushbackDirection.Straight);
            failures += ExpectDirection(parser, "push straight back", PushbackDirection.Straight);
            failures += ExpectDirection(parser, "no turn required", PushbackDirection.Straight);
            failures += ExpectType(parser, "hold pushback", RampCommandType.PushbackHold);
            failures += ExpectType(parser, "resume pushback", RampCommandType.PushbackResume);
            failures += ExpectType(parser, "cancel pushback", RampCommandType.PushbackCancel);
            failures += ExpectType(parser, "disconnect tug", RampCommandType.TugDisconnect);
            failures += ExpectType(parser, "remove towbar", RampCommandType.TowbarRemove);
            failures += ExpectType(parser, "parking brake released", RampCommandType.BrakesReleased);
            failures += ExpectType(parser, "parking brake set", RampCommandType.BrakesSet);
            failures += ExpectType(parser, "confirm brakes set", RampCommandType.BrakesConfirmSet);
            failures += ExpectType(parser, "ready for engine start", RampCommandType.EngineStartReady);
            failures += ExpectType(parser, "starting engine one", RampCommandType.EngineStartEngineOne);
            failures += ExpectType(parser, "starting engine two", RampCommandType.EngineStartEngineTwo);
            failures += ExpectType(parser, "starting both engines", RampCommandType.EngineStartBoth);
            failures += ExpectType(parser, "request deicing", RampCommandType.DeicingRequest);
            failures += ExpectType(parser, "apply type one", RampCommandType.DeicingTypeOne);
            failures += ExpectType(parser, "type one and type four", RampCommandType.DeicingTypeOneAndFour);
            failures += ExpectType(parser, "deice wings and tail", RampCommandType.DeicingWingsAndTail);
            failures += ExpectType(parser, "request follow me", RampCommandType.FollowMeRequest);
            failures += ExpectType(parser, "request marshaller", RampCommandType.MarshallerRequest);
            failures += ExpectType(parser, "cancel service", RampCommandType.ServiceCancel);
            failures += ExpectType(parser, "service status", RampCommandType.ServiceStatus);

            failures += ExpectBlocked(parser, "ground speed alive");
            failures += ExpectBlocked(parser, "boarding music is loud");
            failures += ExpectBlocked(parser, "fuel pump on");
            failures += ExpectBlocked(parser, "fuel flow stable");
            failures += ExpectBlocked(parser, "parking brake test");
            failures += ExpectBlocked(parser, "push to talk");

            failures += ExpectFuel(parser, "fuel to 8.2 tons", 8.2m, "tons");
            failures += ExpectFuel(parser, "fuel to eight point two tons", 8.2m, "tons");
            failures += ExpectFuel(parser, "fuel to 8200 kilos", 8200m, "kilos");
            failures += ExpectFuel(parser, "add 5000 pounds", 5000m, "pounds");

            failures += ExpectOnGroundGuard();
            failures += ExpectAmbiguousMenuSkips();

            Console.WriteLine(failures == 0 ? "Parser tests passed." : ("Parser tests failed: " + failures));
            return failures == 0 ? 0 : 1;
        }

        public static void AddAllRecognizedPhrases(Choices choices)
        {
            foreach (var phrase in RecognizedPhrases)
            {
                choices.Add(phrase);
            }
        }

        private static int ExpectType(RampPhraseParser parser, string phrase, RampCommandType expected)
        {
            var command = parser.Parse(phrase);
            return command.Type == expected ? 0 : Fail("Expected '" + phrase + "' => " + expected + " but got " + command.Type);
        }

        private static int ExpectDirection(RampPhraseParser parser, string phrase, PushbackDirection expected)
        {
            var command = parser.Parse(phrase);
            return command.PushDirection == expected ? 0 : Fail("Expected push direction '" + phrase + "' => " + expected + " but got " + command.PushDirection);
        }

        private static int ExpectBlocked(RampPhraseParser parser, string phrase)
        {
            var command = parser.Parse(phrase);
            return command.Quality == MatchQuality.Blocked ? 0 : Fail("Expected blocked false positive: " + phrase);
        }

        private static int ExpectFuel(RampPhraseParser parser, string phrase, decimal amount, string unit)
        {
            var command = parser.Parse(phrase);
            if (command.FuelRequest == null) return Fail("Expected parsed fuel amount for '" + phrase + "'.");
            if (command.FuelRequest.Amount != amount) return Fail("Expected fuel amount " + amount + " for '" + phrase + "', got " + command.FuelRequest.Amount);
            if (command.FuelRequest.Unit != unit) return Fail("Expected fuel unit " + unit + " for '" + phrase + "', got " + command.FuelRequest.Unit);
            return 0;
        }

        private static int ExpectOnGroundGuard()
        {
            var fakeMenu = new FakeMenuController();
            var processor = new RampCommandProcessor(fakeMenu, false, Console.WriteLine);
            var command = new RampPhraseParser().Parse("start boarding");
            var response = processor.Execute(command, false);
            if (!response.Contains("not on the ground")) return Fail("Expected onGround guard response.");
            if (fakeMenu.OpenCount != 0) return Fail("Expected no GSX interaction when onGround is false.");
            return 0;
        }

        private static int ExpectAmbiguousMenuSkips()
        {
            var fakeMenu = new FakeMenuController();
            fakeMenu.EnqueueOpen(MenuSelectionResult.Ambiguous("Multiple exact GSX menu matches."));
            var processor = new RampCommandProcessor(fakeMenu, false, Console.WriteLine);
            var response = processor.Execute(new RampPhraseParser().Parse("request catering"), true);
            if (!response.Contains("Multiple exact GSX menu matches")) return Fail("Expected ambiguous GSX menu response.");
            if (fakeMenu.SelectionCount != 0) return Fail("Expected no keypress-equivalent selection on ambiguous menu match.");
            return 0;
        }

        private static int Fail(string message)
        {
            Console.WriteLine("FAIL: " + message);
            return 1;
        }

        private static readonly string[] RecognizedPhrases = new[]
        {
            "ramp cockpit","ramp flight deck","ground crew cockpit","ramp do you read","can we get ramp on headset",
            "start boarding","begin boarding","we are ready for boarding","send the passengers","board the passengers",
            "pause boarding","hold boarding","resume boarding","boarding complete","how is boarding going",
            "start deboarding","begin deboarding","start deplaning","let the passengers off","begin disembarkation","deboarding complete","hold deboarding",
            "connect jetway","bring the jetway in","dock the jet bridge","disconnect jetway","pull the jetway back","request stairs","bring stairs","remove stairs","stairs can be removed",
            "start baggage loading","load the bags","begin loading bags","start cargo loading","load cargo","unload bags","start baggage offload","unload cargo","are the bags loaded","baggage complete",
            "request catering","send catering","bring the catering truck","start catering","remove catering","catering complete",
            "request refueling","send the fuel truck","start refueling","fuel to eight point two tons","fuel to 8.2 tons","fuel to 8200 kilos","add 5000 pounds","top us off","stop fueling","disconnect fuel truck","fueling complete",
            "request pushback","ready for push","ready for pushback","call the tug","send the tug","connect the tug","push tail left","tail left please","push facing left","nose right after push","push tail right","tail right please","push facing right","nose left after push","straight pushback","push straight back","no turn required","hold pushback","stop the push","resume pushback","continue push","cancel pushback","disconnect tug","remove towbar",
            "brakes released","parking brake released","brakes are off","parking brake is off","brakes set","parking brake set","brakes are set","confirm brakes set","confirm brakes released",
            "ready to start engines","ready for engine start","starting engine one","starting engine two","starting both engines","engine start complete","both engines stable",
            "request deicing","we need deicing","send the deice truck","start deicing","apply type one","apply type four","type one and type four","deice wings","deice wings and tail","deicing complete","cancel deicing",
            "request follow me","send follow me car","guide us to the stand","guide us to the gate","request marshaller","need a marshaller","marshaller required","marshalling complete",
            "cancel service","cancel current service","stop current service","what is the status","service status","what are we waiting for","hold service","resume service"
        };
    }

    internal sealed class FakeMenuController : IGsxMenuController
    {
        private readonly Queue<MenuSelectionResult> _openResults = new Queue<MenuSelectionResult>();
        private readonly Queue<MenuSelectionResult> _existingResults = new Queue<MenuSelectionResult>();

        public int OpenCount;
        public int SelectionCount;

        public void EnqueueOpen(MenuSelectionResult result)
        {
            _openResults.Enqueue(result);
        }

        public void EnqueueExisting(MenuSelectionResult result)
        {
            _existingResults.Enqueue(result);
        }

        public string GetTooltip()
        {
            return null;
        }

        public IList<string> GetMenuLines()
        {
            return new string[0];
        }

        public MenuSelectionResult OpenAndSelect(string reason, params string[] patterns)
        {
            OpenCount++;
            if (_openResults.Count == 0) return MenuSelectionResult.NotFound("No fake open result queued.");
            var result = _openResults.Dequeue();
            if (result.WasSelected) SelectionCount++;
            return result;
        }

        public MenuSelectionResult TrySelectExisting(params string[] patterns)
        {
            if (_existingResults.Count == 0) return MenuSelectionResult.NotFound("No fake existing result queued.");
            var result = _existingResults.Dequeue();
            if (result.WasSelected) SelectionCount++;
            return result;
        }
    }
}
