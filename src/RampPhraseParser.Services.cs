namespace SimpleOps.GsxRamp
{
    internal sealed partial class RampPhraseParser
    {
        private static bool TryParseRampContact(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if ((text.Contains("ramp") && text.Contains("cockpit")) ||
                (text.Contains("ramp") && text.Contains("headset")) ||
                (text.Contains("ramp") && text.Contains("do you read")) ||
                (text.Contains("ground crew") && text.Contains("cockpit")))
            {
                Fill(command, RampCommandType.RampContact, MatchQuality.Strong, "Ramp contact phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            return false;
        }

        private static bool TryParseBoarding(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (TextUtility.ContainsAny(text, "start boarding", "begin boarding", "ready for boarding", "send the passengers", "board the passengers"))
            {
                Fill(command, RampCommandType.BoardingStart, MatchQuality.Strong, "Boarding start phrase detected.", "boarding", "start boarding", "passengers");
                return true;
            }

            if (TextUtility.ContainsAny(text, "pause boarding", "hold boarding"))
            {
                Fill(command, RampCommandType.BoardingPause, MatchQuality.Strong, "Boarding pause phrase detected.", "pause boarding", "hold boarding");
                return true;
            }

            if (TextUtility.ContainsAny(text, "resume boarding"))
            {
                Fill(command, RampCommandType.BoardingResume, MatchQuality.Strong, "Boarding resume phrase detected.", "resume boarding");
                return true;
            }

            if (TextUtility.ContainsAny(text, "boarding complete"))
            {
                Fill(command, RampCommandType.BoardingComplete, MatchQuality.Strong, "Boarding complete phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            if (TextUtility.ContainsAny(text, "how is boarding going"))
            {
                Fill(command, RampCommandType.BoardingStatus, MatchQuality.Strong, "Boarding status phrase detected.", "boarding status", "status");
                return true;
            }

            return false;
        }

        private static bool TryParseDeboarding(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (TextUtility.ContainsAny(text, "start deboarding", "begin deboarding", "let the passengers off"))
            {
                Fill(command, RampCommandType.DeboardingStart, MatchQuality.Strong, "Deboarding start phrase detected.", "deboarding", "deboarding passengers");
                return true;
            }

            if (TextUtility.ContainsAny(text, "deboarding complete"))
            {
                Fill(command, RampCommandType.DeboardingComplete, MatchQuality.Strong, "Deboarding complete phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            if (TextUtility.ContainsAny(text, "hold deboarding"))
            {
                Fill(command, RampCommandType.DeboardingHold, MatchQuality.Strong, "Deboarding hold phrase detected.", "pause deboarding", "hold deboarding");
                return true;
            }

            return false;
        }

        private static bool TryParseJetwayAndStairs(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (TextUtility.ContainsAny(text, "disconnect jetway", "pull the jetway back"))
            {
                Fill(command, RampCommandType.JetwayDisconnect, MatchQuality.Strong, "Jetway disconnect phrase detected.", "disconnect jetway", "remove jetway");
                return true;
            }

            if (TextUtility.ContainsAny(text, "connect jetway", "bring the jetway in", "dock the jetway"))
            {
                Fill(command, RampCommandType.JetwayConnect, MatchQuality.Strong, "Jetway connect phrase detected.", "jetway", "operate jetway", "dock jetway");
                return true;
            }

            if (TextUtility.ContainsAny(text, "request stairs", "bring stairs"))
            {
                Fill(command, RampCommandType.StairsRequest, MatchQuality.Strong, "Stairs request phrase detected.", "stairs", "passenger stairs");
                return true;
            }

            if (TextUtility.ContainsAny(text, "remove stairs", "stairs can be removed"))
            {
                Fill(command, RampCommandType.StairsRemove, MatchQuality.Strong, "Stairs removal phrase detected.", "remove stairs", "stairs");
                return true;
            }

            return false;
        }

        private static bool TryParseBaggageAndCargo(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (TextUtility.ContainsAny(text, "unload baggage", "start baggage offload"))
            {
                Fill(command, RampCommandType.BaggageUnloadStart, MatchQuality.Strong, "Baggage offload phrase detected.", "unload baggage", "baggage offload", "baggage");
                return true;
            }

            if (TextUtility.ContainsAny(text, "unload cargo"))
            {
                Fill(command, RampCommandType.CargoUnloadStart, MatchQuality.Strong, "Cargo unload phrase detected.", "unload cargo", "cargo");
                return true;
            }

            if (TextUtility.ContainsAny(text, "start baggage loading", "load the baggage", "begin loading baggage"))
            {
                Fill(command, RampCommandType.BaggageLoadStart, MatchQuality.Strong, "Baggage loading phrase detected.", "baggage loading", "load baggage", "baggage");
                return true;
            }

            if (TextUtility.ContainsAny(text, "start cargo loading", "load cargo"))
            {
                Fill(command, RampCommandType.CargoLoadStart, MatchQuality.Strong, "Cargo loading phrase detected.", "cargo loading", "load cargo", "cargo");
                return true;
            }

            if (TextUtility.ContainsAny(text, "are the baggage loaded"))
            {
                Fill(command, RampCommandType.BaggageStatus, MatchQuality.Strong, "Baggage status phrase detected.", "baggage status", "status");
                return true;
            }

            if (TextUtility.ContainsAny(text, "baggage complete"))
            {
                Fill(command, RampCommandType.BaggageComplete, MatchQuality.Strong, "Baggage complete phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            return false;
        }

        private static bool TryParseCatering(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (TextUtility.ContainsAny(text, "request catering", "send catering", "bring the catering", "start catering"))
            {
                Fill(command, RampCommandType.CateringRequest, MatchQuality.Strong, "Catering request phrase detected.", "catering");
                return true;
            }

            if (TextUtility.ContainsAny(text, "remove catering"))
            {
                Fill(command, RampCommandType.CateringRemove, MatchQuality.Strong, "Catering removal phrase detected.", "remove catering", "catering");
                return true;
            }

            if (TextUtility.ContainsAny(text, "catering complete"))
            {
                Fill(command, RampCommandType.CateringComplete, MatchQuality.Strong, "Catering complete phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            return false;
        }
    }
}
