namespace SimpleOps.GsxRamp
{
    internal sealed partial class RampPhraseParser
    {
        private static bool TryParseFuel(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            var fuelRequest = FuelParser.TryParse(command.RawPhrase, text);
            if (fuelRequest != null && TextUtility.ContainsAny(text, "fuel to", "add"))
            {
                Fill(command, RampCommandType.RefuelingTarget, MatchQuality.Strong, "Fuel target phrase detected.", "refueling", "fuel");
                command.FuelRequest = fuelRequest;
                return true;
            }

            if (TextUtility.ContainsAny(text, "request refueling", "send the fuel truck", "start refueling", "top us off"))
            {
                Fill(command, RampCommandType.RefuelingRequest, MatchQuality.Strong, "Refueling request phrase detected.", "refueling", "fuel");
                return true;
            }

            if (TextUtility.ContainsAny(text, "stop fueling"))
            {
                Fill(command, RampCommandType.RefuelingStop, MatchQuality.Strong, "Refueling stop phrase detected.", "stop refueling", "fuel");
                return true;
            }

            if (TextUtility.ContainsAny(text, "disconnect refueling", "disconnect fuel", "disconnect fuel truck"))
            {
                Fill(command, RampCommandType.RefuelingDisconnect, MatchQuality.Strong, "Refueling disconnect phrase detected.", "remove fuel", "fuel");
                return true;
            }

            if (TextUtility.ContainsAny(text, "fueling complete"))
            {
                Fill(command, RampCommandType.RefuelingComplete, MatchQuality.Strong, "Refueling complete phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            return false;
        }

        private static bool TryParsePushback(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (TextUtility.ContainsAny(text, "request pushback", "ready for push", "ready for pushback", "call the tug", "send the tug", "connect the tug"))
            {
                Fill(command, RampCommandType.PushbackRequest, MatchQuality.Strong, "Pushback request phrase detected.", "prepare for pushback", "pushback and departure", "departure");
                command.OpensPushbackSubmenu = true;
                return true;
            }

            if (TextUtility.ContainsAny(text, "push tail left", "tail left please", "push facing left", "nose right after push"))
            {
                Fill(command, RampCommandType.PushbackDirection, MatchQuality.Strong, "Pushback left-turn phrase detected.", "tail left", "nose right", "left");
                command.PushDirection = PushbackDirection.TailLeftNoseRight;
                command.UsesExistingPushbackSubmenu = true;
                return true;
            }

            if (TextUtility.ContainsAny(text, "push tail right", "tail right please", "push facing right", "nose left after push"))
            {
                Fill(command, RampCommandType.PushbackDirection, MatchQuality.Strong, "Pushback right-turn phrase detected.", "tail right", "nose left", "right");
                command.PushDirection = PushbackDirection.TailRightNoseLeft;
                command.UsesExistingPushbackSubmenu = true;
                return true;
            }

            if (TextUtility.ContainsAny(text, "straight pushback", "push straight back", "no turn required"))
            {
                Fill(command, RampCommandType.PushbackDirection, MatchQuality.Strong, "Pushback straight phrase detected.", "straight", "straight back", "no turn");
                command.PushDirection = PushbackDirection.Straight;
                command.UsesExistingPushbackSubmenu = true;
                return true;
            }

            if (TextUtility.ContainsAny(text, "hold pushback", "stop the push"))
            {
                Fill(command, RampCommandType.PushbackHold, MatchQuality.Strong, "Pushback hold phrase detected.", "hold pushback", "stop pushback");
                return true;
            }

            if (TextUtility.ContainsAny(text, "resume pushback", "continue push"))
            {
                Fill(command, RampCommandType.PushbackResume, MatchQuality.Strong, "Pushback resume phrase detected.", "resume pushback", "continue pushback");
                return true;
            }

            if (TextUtility.ContainsAny(text, "cancel pushback"))
            {
                Fill(command, RampCommandType.PushbackCancel, MatchQuality.Strong, "Pushback cancel phrase detected.", "cancel pushback");
                return true;
            }

            if (TextUtility.ContainsAny(text, "disconnect tug"))
            {
                Fill(command, RampCommandType.TugDisconnect, MatchQuality.Strong, "Tug disconnect phrase detected.", "disconnect tug", "tug");
                return true;
            }

            if (TextUtility.ContainsAny(text, "remove towbar"))
            {
                Fill(command, RampCommandType.TowbarRemove, MatchQuality.Strong, "Towbar removal phrase detected.", "remove towbar", "towbar");
                return true;
            }

            return false;
        }

        private static bool TryParseBrakes(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (TextUtility.ContainsAny(text, "confirm brakes set"))
            {
                Fill(command, RampCommandType.BrakesConfirmSet, MatchQuality.Strong, "Brakes confirm-set phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            if (TextUtility.ContainsAny(text, "confirm brakes released"))
            {
                Fill(command, RampCommandType.BrakesConfirmReleased, MatchQuality.Strong, "Brakes confirm-released phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            if (TextUtility.ContainsAny(text, "brakes released", "parking brake released", "brakes are off", "parking brake is off"))
            {
                Fill(command, RampCommandType.BrakesReleased, MatchQuality.Strong, "Brakes released phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            if (TextUtility.ContainsAny(text, "brakes set", "parking brake set", "brakes are set"))
            {
                Fill(command, RampCommandType.BrakesSet, MatchQuality.Strong, "Brakes set phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            return false;
        }

        private static bool TryParseEngineStart(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (TextUtility.ContainsAny(text, "ready to start engines", "ready for engine start"))
            {
                Fill(command, RampCommandType.EngineStartReady, MatchQuality.Strong, "Engine start ready phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            if (TextUtility.ContainsAny(text, "starting engine one"))
            {
                Fill(command, RampCommandType.EngineStartEngineOne, MatchQuality.Strong, "Engine one start phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            if (TextUtility.ContainsAny(text, "starting engine two"))
            {
                Fill(command, RampCommandType.EngineStartEngineTwo, MatchQuality.Strong, "Engine two start phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            if (TextUtility.ContainsAny(text, "starting both engines"))
            {
                Fill(command, RampCommandType.EngineStartBoth, MatchQuality.Strong, "Both engines start phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            if (TextUtility.ContainsAny(text, "engine start complete"))
            {
                Fill(command, RampCommandType.EngineStartComplete, MatchQuality.Strong, "Engine start complete phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            if (TextUtility.ContainsAny(text, "both engines stable"))
            {
                Fill(command, RampCommandType.EnginesStable, MatchQuality.Strong, "Both engines stable phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            return false;
        }
    }
}
