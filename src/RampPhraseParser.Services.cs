namespace SimpleOps.GsxRamp
{
    internal sealed partial class RampPhraseParser
    {
        private bool TryParseRampContact(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (ContainsAny(text, "RampContact", "ramp cockpit", "ramp flight deck", "ground crew cockpit", "ramp do you read", "can we get ramp on headset", "ground crew do you read", "can we get ground on headset"))
            {
                Fill(command, RampCommandType.RampContact, MatchQuality.Strong, "Ramp contact phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            return false;
        }

        private bool TryParseBoarding(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (ContainsAny(text, "BoardingStart", "start boarding", "begin boarding", "ready for boarding", "we are ready for boarding", "send the passengers", "board the passengers", "boarding may begin", "ready to board passengers"))
            {
                Fill(command, RampCommandType.BoardingStart, MatchQuality.Strong, "Boarding start phrase detected.", "boarding", "start boarding", "passengers");
                return true;
            }

            if (ContainsAny(text, "BoardingPause", "pause boarding", "hold boarding", "stop boarding"))
            {
                Fill(command, RampCommandType.BoardingPause, MatchQuality.Strong, "Boarding pause phrase detected.", "pause boarding", "hold boarding");
                return true;
            }

            if (ContainsAny(text, "BoardingResume", "resume boarding", "continue boarding"))
            {
                Fill(command, RampCommandType.BoardingResume, MatchQuality.Strong, "Boarding resume phrase detected.", "resume boarding");
                return true;
            }

            if (ContainsAny(text, "BoardingComplete", "boarding complete"))
            {
                Fill(command, RampCommandType.BoardingComplete, MatchQuality.Strong, "Boarding complete phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            if (ContainsAny(text, "BoardingStatus", "how is boarding going", "boarding status"))
            {
                Fill(command, RampCommandType.BoardingStatus, MatchQuality.Strong, "Boarding status phrase detected.", "boarding status", "status");
                return true;
            }

            return false;
        }

        private bool TryParseDeboarding(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (ContainsAny(text, "DeboardingStart", "start deboarding", "begin deboarding", "start deplaning", "let the passengers off", "begin disembarkation", "start passenger deboarding"))
            {
                Fill(command, RampCommandType.DeboardingStart, MatchQuality.Strong, "Deboarding start phrase detected.", "deboarding", "deboarding passengers");
                return true;
            }

            if (ContainsAny(text, "DeboardingComplete", "deboarding complete"))
            {
                Fill(command, RampCommandType.DeboardingComplete, MatchQuality.Strong, "Deboarding complete phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            if (ContainsAny(text, "DeboardingHold", "hold deboarding", "pause deboarding"))
            {
                Fill(command, RampCommandType.DeboardingHold, MatchQuality.Strong, "Deboarding hold phrase detected.", "pause deboarding", "hold deboarding");
                return true;
            }

            return false;
        }

        private bool TryParseJetwayAndStairs(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (ContainsAny(text, "JetwayDisconnect", "disconnect jetway", "pull the jetway back", "remove the jetway"))
            {
                Fill(command, RampCommandType.JetwayDisconnect, MatchQuality.Strong, "Jetway disconnect phrase detected.", "disconnect jetway", "remove jetway");
                return true;
            }

            if (ContainsAny(text, "JetwayConnect", "connect jetway", "bring the jetway in", "dock the jet bridge", "dock the jetway"))
            {
                Fill(command, RampCommandType.JetwayConnect, MatchQuality.Strong, "Jetway connect phrase detected.", "jetway", "operate jetway", "dock jetway");
                return true;
            }

            if (ContainsAny(text, "StairsRequest", "request stairs", "bring stairs", "send stairs"))
            {
                Fill(command, RampCommandType.StairsRequest, MatchQuality.Strong, "Stairs request phrase detected.", "stairs", "passenger stairs");
                return true;
            }

            if (ContainsAny(text, "StairsRemove", "remove stairs", "stairs can be removed"))
            {
                Fill(command, RampCommandType.StairsRemove, MatchQuality.Strong, "Stairs removal phrase detected.", "remove stairs", "stairs");
                return true;
            }

            return false;
        }

        private bool TryParseBaggageAndCargo(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (ContainsAny(text, "BaggageUnloadStart", "unload baggage", "unload bags", "start baggage offload"))
            {
                Fill(command, RampCommandType.BaggageUnloadStart, MatchQuality.Strong, "Baggage offload phrase detected.", "unload baggage", "baggage offload", "baggage");
                return true;
            }

            if (ContainsAny(text, "CargoUnloadStart", "unload cargo", "start cargo offload"))
            {
                Fill(command, RampCommandType.CargoUnloadStart, MatchQuality.Strong, "Cargo unload phrase detected.", "unload cargo", "cargo");
                return true;
            }

            if (ContainsAny(text, "BaggageLoadStart", "start baggage loading", "load the baggage", "load the bags", "begin loading baggage", "begin loading bags", "load baggage"))
            {
                Fill(command, RampCommandType.BaggageLoadStart, MatchQuality.Strong, "Baggage loading phrase detected.", "baggage loading", "load baggage", "baggage");
                return true;
            }

            if (ContainsAny(text, "CargoLoadStart", "start cargo loading", "load cargo", "start cargo service"))
            {
                Fill(command, RampCommandType.CargoLoadStart, MatchQuality.Strong, "Cargo loading phrase detected.", "cargo loading", "load cargo", "cargo");
                return true;
            }

            if (ContainsAny(text, "BaggageStatus", "are the bags loaded", "are the baggage loaded"))
            {
                Fill(command, RampCommandType.BaggageStatus, MatchQuality.Strong, "Baggage status phrase detected.", "baggage status", "status");
                return true;
            }

            if (ContainsAny(text, "BaggageComplete", "baggage complete", "cargo complete"))
            {
                Fill(command, RampCommandType.BaggageComplete, MatchQuality.Strong, "Baggage complete phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            return false;
        }

        private bool TryParseCatering(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (ContainsAny(text, "CateringRequest", "request catering", "send catering", "bring the catering", "bring the catering truck", "start catering"))
            {
                Fill(command, RampCommandType.CateringRequest, MatchQuality.Strong, "Catering request phrase detected.", "catering");
                return true;
            }

            if (ContainsAny(text, "CateringRemove", "remove catering"))
            {
                Fill(command, RampCommandType.CateringRemove, MatchQuality.Strong, "Catering removal phrase detected.", "remove catering", "catering");
                return true;
            }

            if (ContainsAny(text, "CateringComplete", "catering complete", "catering can leave"))
            {
                Fill(command, RampCommandType.CateringComplete, MatchQuality.Strong, "Catering complete phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            return false;
        }
    }
}
