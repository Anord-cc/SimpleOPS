// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SimpleOps.GsxRamp
{
    internal static class ParserTestHarness
    {
        public static int Run(Action<string> log = null)
        {
            log = log ?? Console.WriteLine;
            int failures = 0;
            var parser = new RampPhraseParser();

            foreach (var test in CommandExpectations)
            {
                failures += ExpectType(log, parser, test.Phrase, test.ExpectedType);
            }

            foreach (var falsePositive in FalsePositives)
            {
                failures += ExpectBlocked(log, parser, falsePositive);
            }

            failures += ExpectFuel(log, parser, "fuel to 8.2 tons", 8.2m, "tons");
            failures += ExpectFuel(log, parser, "fuel to eight point two tons", 8.2m, "tons");
            failures += ExpectFuel(log, parser, "fuel to 8200 kilos", 8200m, "kilos");
            failures += ExpectFuel(log, parser, "add 5000 pounds", 5000m, "pounds");

            failures += ExpectDirection(log, parser, "tail left please", PushbackDirection.TailLeftNoseRight);
            failures += ExpectDirection(log, parser, "nose right after push", PushbackDirection.TailLeftNoseRight);
            failures += ExpectDirection(log, parser, "tail right please", PushbackDirection.TailRightNoseLeft);
            failures += ExpectDirection(log, parser, "nose left after push", PushbackDirection.TailRightNoseLeft);
            failures += ExpectDirection(log, parser, "push straight back", PushbackDirection.Straight);

            failures += ExpectAliasOverlay(log);
            failures += ExpectOnGroundGuard(log);
            failures += ExpectAmbiguousMenuSkips(log);
            failures += ExpectMenuMatching(log);
            failures += ExpectVoiceFallback(log);
            failures += ExpectSettingsFallback(log);

            log(failures == 0 ? "Parser tests passed." : ("Parser tests failed: " + failures));
            return failures == 0 ? 0 : 1;
        }

        private static int ExpectType(Action<string> log, RampPhraseParser parser, string phrase, RampCommandType expected)
        {
            var command = parser.Parse(phrase);
            return command.Type == expected ? 0 : Fail(log, "Expected '" + phrase + "' => " + expected + " but got " + command.Type);
        }

        private static int ExpectDirection(Action<string> log, RampPhraseParser parser, string phrase, PushbackDirection expected)
        {
            var command = parser.Parse(phrase);
            return command.PushDirection == expected ? 0 : Fail(log, "Expected push direction '" + phrase + "' => " + expected + " but got " + command.PushDirection);
        }

        private static int ExpectBlocked(Action<string> log, RampPhraseParser parser, string phrase)
        {
            var command = parser.Parse(phrase);
            return command.Quality == MatchQuality.Blocked ? 0 : Fail(log, "Expected blocked false positive: " + phrase);
        }

        private static int ExpectFuel(Action<string> log, RampPhraseParser parser, string phrase, decimal amount, string unit)
        {
            var command = parser.Parse(phrase);
            if (command.FuelRequest == null)
            {
                return Fail(log, "Expected parsed fuel amount for '" + phrase + "'.");
            }

            if (command.FuelRequest.Amount != amount)
            {
                return Fail(log, "Expected fuel amount " + amount + " for '" + phrase + "', got " + command.FuelRequest.Amount);
            }

            if (command.FuelRequest.Unit != unit)
            {
                return Fail(log, "Expected fuel unit " + unit + " for '" + phrase + "', got " + command.FuelRequest.Unit);
            }

            return 0;
        }

        private static int ExpectAliasOverlay(Action<string> log)
        {
            var aliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "BoardingStart", new[] { "commence boarding now" } }
            };

            var parser = new RampPhraseParser(aliases);
            return ExpectType(log, parser, "commence boarding now", RampCommandType.BoardingStart);
        }

        private static int ExpectOnGroundGuard(Action<string> log)
        {
            var fakeMenu = new FakeMenuController();
            var processor = new RampCommandProcessor(fakeMenu, false, log);
            var command = new RampPhraseParser().Parse("start boarding");
            var response = processor.Execute(command, false);
            if (!response.Contains("not on the ground"))
            {
                return Fail(log, "Expected onGround guard response.");
            }

            if (fakeMenu.OpenCount != 0)
            {
                return Fail(log, "Expected no GSX interaction when onGround is false.");
            }

            return 0;
        }

        private static int ExpectAmbiguousMenuSkips(Action<string> log)
        {
            var fakeMenu = new FakeMenuController();
            fakeMenu.EnqueueOpen(MenuSelectionResult.Ambiguous("Multiple exact GSX menu matches."));
            var processor = new RampCommandProcessor(fakeMenu, false, log);
            var response = processor.Execute(new RampPhraseParser().Parse("request catering"), true);
            if (!response.Contains("Multiple exact GSX menu matches"))
            {
                return Fail(log, "Expected ambiguous GSX menu response.");
            }

            if (fakeMenu.SelectionCount != 0)
            {
                return Fail(log, "Expected no selection on ambiguous menu match.");
            }

            return 0;
        }

        private static int ExpectMenuMatching(Action<string> log)
        {
            var exact = GsxMenuDriver.MatchMenuChoice(new[] { "GSX", "Request catering", "Request refueling" }, "request catering");
            if (exact.Status != MenuMatchStatus.Selected)
            {
                return Fail(log, "Expected exact menu match to succeed.");
            }

            var synonym = GsxMenuDriver.MatchMenuChoice(new[] { "GSX", "Operate jet bridge" }, "connect jetway");
            if (synonym.Status != MenuMatchStatus.Selected)
            {
                return Fail(log, "Expected synonym menu match to succeed.");
            }

            var ambiguous = GsxMenuDriver.MatchMenuChoice(new[] { "GSX", "Request catering", "Remove catering" }, "catering");
            if (ambiguous.Status != MenuMatchStatus.Ambiguous)
            {
                return Fail(log, "Expected ambiguous menu match to be reported.");
            }

            return 0;
        }

        private static int ExpectVoiceFallback(Action<string> log)
        {
            var paths = AppPaths.Create();
            var settings = AppSettings.CreateDefault();
            settings.OpenAiVoiceEnabled = true;
            var voice = new OpenAiVoiceOutputService(settings, new FakeCredentialStore(null), paths, log);
            try
            {
                if (!voice.StatusText.Contains("API key missing"))
                {
                    return Fail(log, "Expected OpenAI voice to disable cleanly when the API key is missing.");
                }
            }
            finally
            {
                voice.Dispose();
            }

            return 0;
        }

        private static int ExpectSettingsFallback(Action<string> log)
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "SimpleOpsTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                var paths = new AppPaths
                {
                    RootDirectory = tempRoot,
                    SettingsPath = Path.Combine(tempRoot, "settings.json"),
                    PhraseAliasPath = Path.Combine(tempRoot, "phrases.json"),
                    LogDirectory = Path.Combine(tempRoot, "logs"),
                    VoiceCacheDirectory = Path.Combine(tempRoot, "voice-cache")
                };

                Directory.CreateDirectory(paths.LogDirectory);
                Directory.CreateDirectory(paths.VoiceCacheDirectory);
                File.WriteAllText(paths.SettingsPath, "{not-json");
                var store = new JsonSettingsStore(paths, log);
                var loaded = store.Load();
                if (loaded == null || string.IsNullOrWhiteSpace(loaded.TelemetryUrl))
                {
                    return Fail(log, "Expected invalid settings JSON to fall back to defaults.");
                }
            }
            finally
            {
                try
                {
                    Directory.Delete(tempRoot, true);
                }
                catch
                {
                }
            }

            return 0;
        }

        private static int Fail(Action<string> log, string message)
        {
            log("FAIL: " + message);
            return 1;
        }

        private sealed class FakeCredentialStore : ICredentialStore
        {
            private string _secret;

            public FakeCredentialStore(string secret)
            {
                _secret = secret;
            }

            public string GetSecret(string key)
            {
                return _secret;
            }

            public void SaveSecret(string key, string secret)
            {
                _secret = secret;
            }

            public void DeleteSecret(string key)
            {
                _secret = null;
            }
        }

        private static readonly CommandExpectation[] CommandExpectations = new[]
        {
            new CommandExpectation("ramp cockpit", RampCommandType.RampContact),
            new CommandExpectation("ramp flight deck", RampCommandType.RampContact),
            new CommandExpectation("ground crew cockpit", RampCommandType.RampContact),
            new CommandExpectation("ramp do you read", RampCommandType.RampContact),
            new CommandExpectation("can we get ramp on headset", RampCommandType.RampContact),
            new CommandExpectation("ground crew do you read", RampCommandType.RampContact),
            new CommandExpectation("start boarding", RampCommandType.BoardingStart),
            new CommandExpectation("begin boarding", RampCommandType.BoardingStart),
            new CommandExpectation("we are ready for boarding", RampCommandType.BoardingStart),
            new CommandExpectation("send the passengers", RampCommandType.BoardingStart),
            new CommandExpectation("board the passengers", RampCommandType.BoardingStart),
            new CommandExpectation("boarding may begin", RampCommandType.BoardingStart),
            new CommandExpectation("ready to board passengers", RampCommandType.BoardingStart),
            new CommandExpectation("pause boarding", RampCommandType.BoardingPause),
            new CommandExpectation("hold boarding", RampCommandType.BoardingPause),
            new CommandExpectation("stop boarding", RampCommandType.BoardingPause),
            new CommandExpectation("resume boarding", RampCommandType.BoardingResume),
            new CommandExpectation("continue boarding", RampCommandType.BoardingResume),
            new CommandExpectation("boarding complete", RampCommandType.BoardingComplete),
            new CommandExpectation("how is boarding going", RampCommandType.BoardingStatus),
            new CommandExpectation("boarding status", RampCommandType.BoardingStatus),
            new CommandExpectation("start deboarding", RampCommandType.DeboardingStart),
            new CommandExpectation("begin deboarding", RampCommandType.DeboardingStart),
            new CommandExpectation("start deplaning", RampCommandType.DeboardingStart),
            new CommandExpectation("let the passengers off", RampCommandType.DeboardingStart),
            new CommandExpectation("begin disembarkation", RampCommandType.DeboardingStart),
            new CommandExpectation("deboarding complete", RampCommandType.DeboardingComplete),
            new CommandExpectation("hold deboarding", RampCommandType.DeboardingHold),
            new CommandExpectation("pause deboarding", RampCommandType.DeboardingHold),
            new CommandExpectation("connect jetway", RampCommandType.JetwayConnect),
            new CommandExpectation("bring the jetway in", RampCommandType.JetwayConnect),
            new CommandExpectation("dock the jet bridge", RampCommandType.JetwayConnect),
            new CommandExpectation("disconnect jetway", RampCommandType.JetwayDisconnect),
            new CommandExpectation("pull the jetway back", RampCommandType.JetwayDisconnect),
            new CommandExpectation("request stairs", RampCommandType.StairsRequest),
            new CommandExpectation("bring stairs", RampCommandType.StairsRequest),
            new CommandExpectation("send stairs", RampCommandType.StairsRequest),
            new CommandExpectation("remove stairs", RampCommandType.StairsRemove),
            new CommandExpectation("stairs can be removed", RampCommandType.StairsRemove),
            new CommandExpectation("start baggage loading", RampCommandType.BaggageLoadStart),
            new CommandExpectation("load the bags", RampCommandType.BaggageLoadStart),
            new CommandExpectation("begin loading bags", RampCommandType.BaggageLoadStart),
            new CommandExpectation("load baggage", RampCommandType.BaggageLoadStart),
            new CommandExpectation("start cargo loading", RampCommandType.CargoLoadStart),
            new CommandExpectation("load cargo", RampCommandType.CargoLoadStart),
            new CommandExpectation("start cargo service", RampCommandType.CargoLoadStart),
            new CommandExpectation("unload bags", RampCommandType.BaggageUnloadStart),
            new CommandExpectation("start baggage offload", RampCommandType.BaggageUnloadStart),
            new CommandExpectation("unload cargo", RampCommandType.CargoUnloadStart),
            new CommandExpectation("start cargo offload", RampCommandType.CargoUnloadStart),
            new CommandExpectation("are the bags loaded", RampCommandType.BaggageStatus),
            new CommandExpectation("baggage complete", RampCommandType.BaggageComplete),
            new CommandExpectation("request catering", RampCommandType.CateringRequest),
            new CommandExpectation("send catering", RampCommandType.CateringRequest),
            new CommandExpectation("bring the catering truck", RampCommandType.CateringRequest),
            new CommandExpectation("start catering", RampCommandType.CateringRequest),
            new CommandExpectation("remove catering", RampCommandType.CateringRemove),
            new CommandExpectation("catering complete", RampCommandType.CateringComplete),
            new CommandExpectation("request refueling", RampCommandType.RefuelingRequest),
            new CommandExpectation("send the fuel truck", RampCommandType.RefuelingRequest),
            new CommandExpectation("start refueling", RampCommandType.RefuelingRequest),
            new CommandExpectation("top us off", RampCommandType.RefuelingRequest),
            new CommandExpectation("fuel to eight point two tons", RampCommandType.RefuelingTarget),
            new CommandExpectation("fuel to 8.2 tons", RampCommandType.RefuelingTarget),
            new CommandExpectation("fuel to 8200 kilos", RampCommandType.RefuelingTarget),
            new CommandExpectation("fuel to 8200 kilograms", RampCommandType.RefuelingTarget),
            new CommandExpectation("add 5000 pounds", RampCommandType.RefuelingTarget),
            new CommandExpectation("stop fueling", RampCommandType.RefuelingStop),
            new CommandExpectation("disconnect fuel truck", RampCommandType.RefuelingDisconnect),
            new CommandExpectation("fueling complete", RampCommandType.RefuelingComplete),
            new CommandExpectation("request pushback", RampCommandType.PushbackRequest),
            new CommandExpectation("ready for push", RampCommandType.PushbackRequest),
            new CommandExpectation("ready for pushback", RampCommandType.PushbackRequest),
            new CommandExpectation("call the tug", RampCommandType.PushbackRequest),
            new CommandExpectation("send the tug", RampCommandType.PushbackRequest),
            new CommandExpectation("connect the tug", RampCommandType.PushbackRequest),
            new CommandExpectation("push tail left", RampCommandType.PushbackDirection),
            new CommandExpectation("push tail right", RampCommandType.PushbackDirection),
            new CommandExpectation("straight pushback", RampCommandType.PushbackDirection),
            new CommandExpectation("hold pushback", RampCommandType.PushbackHold),
            new CommandExpectation("stop the push", RampCommandType.PushbackHold),
            new CommandExpectation("resume pushback", RampCommandType.PushbackResume),
            new CommandExpectation("continue push", RampCommandType.PushbackResume),
            new CommandExpectation("cancel pushback", RampCommandType.PushbackCancel),
            new CommandExpectation("disconnect tug", RampCommandType.TugDisconnect),
            new CommandExpectation("remove towbar", RampCommandType.TowbarRemove),
            new CommandExpectation("brakes released", RampCommandType.BrakesReleased),
            new CommandExpectation("parking brake released", RampCommandType.BrakesReleased),
            new CommandExpectation("brakes set", RampCommandType.BrakesSet),
            new CommandExpectation("parking brake set", RampCommandType.BrakesSet),
            new CommandExpectation("confirm brakes set", RampCommandType.BrakesConfirmSet),
            new CommandExpectation("confirm brakes released", RampCommandType.BrakesConfirmReleased),
            new CommandExpectation("ready to start engines", RampCommandType.EngineStartReady),
            new CommandExpectation("ready for engine start", RampCommandType.EngineStartReady),
            new CommandExpectation("starting engine one", RampCommandType.EngineStartEngineOne),
            new CommandExpectation("engine one is coming up", RampCommandType.EngineStartEngineOne),
            new CommandExpectation("starting engine two", RampCommandType.EngineStartEngineTwo),
            new CommandExpectation("engine two is coming up", RampCommandType.EngineStartEngineTwo),
            new CommandExpectation("starting both engines", RampCommandType.EngineStartBoth),
            new CommandExpectation("engine start complete", RampCommandType.EngineStartComplete),
            new CommandExpectation("both engines stable", RampCommandType.EnginesStable),
            new CommandExpectation("request deicing", RampCommandType.DeicingRequest),
            new CommandExpectation("we need deicing", RampCommandType.DeicingRequest),
            new CommandExpectation("send the deice truck", RampCommandType.DeicingRequest),
            new CommandExpectation("start deicing", RampCommandType.DeicingStart),
            new CommandExpectation("apply type one", RampCommandType.DeicingTypeOne),
            new CommandExpectation("apply type four", RampCommandType.DeicingTypeFour),
            new CommandExpectation("type one and type four", RampCommandType.DeicingTypeOneAndFour),
            new CommandExpectation("deice wings", RampCommandType.DeicingWings),
            new CommandExpectation("deice wings and tail", RampCommandType.DeicingWingsAndTail),
            new CommandExpectation("deicing complete", RampCommandType.DeicingComplete),
            new CommandExpectation("cancel deicing", RampCommandType.DeicingCancel),
            new CommandExpectation("request follow me", RampCommandType.FollowMeRequest),
            new CommandExpectation("send follow me car", RampCommandType.FollowMeRequest),
            new CommandExpectation("guide us to the stand", RampCommandType.FollowMeRequest),
            new CommandExpectation("guide us to the gate", RampCommandType.FollowMeRequest),
            new CommandExpectation("request marshaller", RampCommandType.MarshallerRequest),
            new CommandExpectation("need a marshaller", RampCommandType.MarshallerRequest),
            new CommandExpectation("marshaller required", RampCommandType.MarshallerRequest),
            new CommandExpectation("marshalling complete", RampCommandType.MarshallingComplete),
            new CommandExpectation("cancel service", RampCommandType.ServiceCancel),
            new CommandExpectation("cancel current service", RampCommandType.ServiceCancel),
            new CommandExpectation("stop current service", RampCommandType.ServiceCancel),
            new CommandExpectation("what is the status", RampCommandType.ServiceStatus),
            new CommandExpectation("service status", RampCommandType.ServiceStatus),
            new CommandExpectation("what are we waiting for", RampCommandType.ServiceStatus),
            new CommandExpectation("hold service", RampCommandType.ServiceHold),
            new CommandExpectation("resume service", RampCommandType.ServiceResume)
        };

        private static readonly string[] FalsePositives =
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
    }

    internal sealed class FakeMenuController : IGsxMenuController
    {
        private readonly Queue<MenuSelectionResult> _openResults = new Queue<MenuSelectionResult>();
        private readonly Queue<MenuSelectionResult> _existingResults = new Queue<MenuSelectionResult>();

        public int OpenCount;
        public int SelectionCount;

        public string StatusText
        {
            get { return "Fake GSX controller."; }
        }

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
            if (_openResults.Count == 0)
            {
                return MenuSelectionResult.NotFound("No fake open result queued.");
            }

            var result = _openResults.Dequeue();
            if (result.WasSelected)
            {
                SelectionCount++;
            }

            return result;
        }

        public MenuSelectionResult TrySelectExisting(params string[] patterns)
        {
            if (_existingResults.Count == 0)
            {
                return MenuSelectionResult.NotFound("No fake existing result queued.");
            }

            var result = _existingResults.Dequeue();
            if (result.WasSelected)
            {
                SelectionCount++;
            }

            return result;
        }
    }

    internal sealed class CommandExpectation
    {
        public readonly string Phrase;
        public readonly RampCommandType ExpectedType;

        public CommandExpectation(string phrase, RampCommandType expectedType)
        {
            Phrase = phrase;
            ExpectedType = expectedType;
        }
    }
}
