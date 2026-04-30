using System;

namespace SimpleOps.GsxRamp
{
    internal sealed class RampCommandProcessor
    {
        private readonly IGsxMenuController _menuController;
        private readonly Action<string> _log;
        private readonly bool _dryRun;
        private DateTime _pushbackSubmenuUntilUtc = DateTime.MinValue;

        public RampCommandProcessor(IGsxMenuController menuController, bool dryRun, Action<string> log)
        {
            _menuController = menuController;
            _dryRun = dryRun;
            _log = log;
        }

        public string Execute(RampCommand command, bool armed)
        {
            if (!armed)
            {
                _log("Ignoring command because onGround gate is inactive.");
                return "Ramp is inactive because the aircraft is not on the ground.";
            }

            if (command == null)
            {
                _log("Ignoring null command.");
                return "Command was empty.";
            }

            if (command.Quality == MatchQuality.Blocked)
            {
                _log("Ignoring blocked phrase: " + command.Reason);
                return "Ignored blocked phrase.";
            }

            if (!command.IsSafeToExecute)
            {
                _log("Ignoring low-confidence or unknown phrase: " + command.Reason);
                return "Phrase ignored.";
            }

            if (command.IsActionableGsx && command.Quality != MatchQuality.Strong)
            {
                _log("Ignoring non-strong GSX command: " + command.Reason);
                return "Phrase ignored because the GSX match was not strong enough.";
            }

            if (!command.IsActionableGsx)
            {
                return BuildNonGsxResponse(command);
            }

            if (_dryRun)
            {
                _log("Dry-run would execute " + command.Type + " from phrase '" + command.RawPhrase + "'.");
                return "Dry run: " + DescribeCommand(command);
            }

            if (command.Type == RampCommandType.PushbackDirection)
            {
                return ExecutePushbackDirection(command);
            }

            var selection = _menuController.OpenAndSelect(command.Type.ToString(), command.MenuPatterns);
            return BuildMenuResponse(command, selection);
        }

        private string ExecutePushbackDirection(RampCommand command)
        {
            MenuSelectionResult selection = null;
            if (DateTime.UtcNow < _pushbackSubmenuUntilUtc)
            {
                selection = _menuController.TrySelectExisting(command.MenuPatterns);
            }

            if (selection == null || !selection.WasSelected)
            {
                var prepare = _menuController.OpenAndSelect("Prepare pushback", "prepare for pushback", "pushback and departure", "departure");
                if (!prepare.WasSelected)
                {
                    return BuildMenuResponse(command, prepare);
                }

                _pushbackSubmenuUntilUtc = DateTime.UtcNow.AddMinutes(2);
                selection = _menuController.TrySelectExisting(command.MenuPatterns);
            }

            _pushbackSubmenuUntilUtc = DateTime.MinValue;
            if (selection == null || !selection.WasSelected)
            {
                _log("Pushback submenu did not yield a unique direction match.");
                return "Pushback direction was ambiguous or missing.";
            }

            return FirstNonEmpty(_menuController.GetTooltip(), "Pushback " + DescribeDirection(command.PushDirection) + " selected.");
        }

        private string BuildMenuResponse(RampCommand command, MenuSelectionResult selection)
        {
            if (selection == null)
            {
                _log("Menu controller returned no result for " + command.Type + ".");
                return "GSX menu was not detected.";
            }

            if (selection.WasSelected)
            {
                if (command.OpensPushbackSubmenu)
                {
                    _pushbackSubmenuUntilUtc = DateTime.UtcNow.AddMinutes(2);
                }

                return FirstNonEmpty(_menuController.GetTooltip(), DescribeCommand(command) + " sent to GSX.");
            }

            _log("GSX action skipped for " + command.Type + ": " + selection.Reason);
            return selection.Reason;
        }

        private string BuildNonGsxResponse(RampCommand command)
        {
            switch (command.Type)
            {
                case RampCommandType.RampContact:
                    return "Ramp here, go ahead.";
                case RampCommandType.BoardingComplete:
                case RampCommandType.DeboardingComplete:
                case RampCommandType.BaggageComplete:
                case RampCommandType.CateringComplete:
                case RampCommandType.RefuelingComplete:
                case RampCommandType.DeicingComplete:
                case RampCommandType.MarshallingComplete:
                    return "Copied.";
                case RampCommandType.BrakesReleased:
                case RampCommandType.BrakesConfirmReleased:
                    return "Brakes released acknowledged.";
                case RampCommandType.BrakesSet:
                case RampCommandType.BrakesConfirmSet:
                    return "Brakes set acknowledged.";
                case RampCommandType.EngineStartReady:
                case RampCommandType.EngineStartEngineOne:
                case RampCommandType.EngineStartEngineTwo:
                case RampCommandType.EngineStartBoth:
                case RampCommandType.EngineStartComplete:
                case RampCommandType.EnginesStable:
                    return "Engine start call acknowledged.";
                default:
                    if (command.Type == RampCommandType.RefuelingTarget && command.FuelRequest != null)
                    {
                        return "Fuel target noted: " + command.FuelRequest.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + command.FuelRequest.Unit + ".";
                    }

                    return DescribeCommand(command) + " acknowledged.";
            }
        }

        private static string DescribeCommand(RampCommand command)
        {
            if (command.Type == RampCommandType.RefuelingTarget && command.FuelRequest != null)
            {
                return "fuel target " + command.FuelRequest.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + command.FuelRequest.Unit;
            }

            return command.Type.ToString();
        }

        private static string DescribeDirection(PushbackDirection direction)
        {
            switch (direction)
            {
                case PushbackDirection.TailLeftNoseRight:
                    return "tail left / nose right";
                case PushbackDirection.TailRightNoseLeft:
                    return "tail right / nose left";
                case PushbackDirection.Straight:
                    return "straight";
                default:
                    return "requested";
            }
        }

        private static string FirstNonEmpty(string first, string fallback)
        {
            return string.IsNullOrWhiteSpace(first) ? fallback : first;
        }
    }
}
