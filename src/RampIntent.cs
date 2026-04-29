using System;

namespace SimpleOps.GsxRamp
{
    internal enum MatchQuality
    {
        None = 0,
        Weak = 1,
        Medium = 2,
        Strong = 3,
        Ambiguous = 4,
        Blocked = 5
    }

    internal enum PushbackDirection
    {
        None = 0,
        TailLeftNoseRight = 1,
        TailRightNoseLeft = 2,
        Straight = 3
    }

    internal enum RampCommandType
    {
        Unknown = 0,
        RampContact,
        BoardingStart,
        BoardingPause,
        BoardingResume,
        BoardingComplete,
        BoardingStatus,
        DeboardingStart,
        DeboardingHold,
        DeboardingComplete,
        JetwayConnect,
        JetwayDisconnect,
        StairsRequest,
        StairsRemove,
        BaggageLoadStart,
        CargoLoadStart,
        BaggageUnloadStart,
        CargoUnloadStart,
        BaggageStatus,
        BaggageComplete,
        CateringRequest,
        CateringRemove,
        CateringComplete,
        RefuelingRequest,
        RefuelingTarget,
        RefuelingStop,
        RefuelingDisconnect,
        RefuelingComplete,
        PushbackRequest,
        PushbackDirection,
        PushbackHold,
        PushbackResume,
        PushbackCancel,
        TugDisconnect,
        TowbarRemove,
        BrakesReleased,
        BrakesSet,
        BrakesConfirmSet,
        BrakesConfirmReleased,
        EngineStartReady,
        EngineStartEngineOne,
        EngineStartEngineTwo,
        EngineStartBoth,
        EngineStartComplete,
        EnginesStable,
        DeicingRequest,
        DeicingStart,
        DeicingTypeOne,
        DeicingTypeFour,
        DeicingTypeOneAndFour,
        DeicingWings,
        DeicingWingsAndTail,
        DeicingComplete,
        DeicingCancel,
        FollowMeRequest,
        MarshallerRequest,
        MarshallingComplete,
        ServiceCancel,
        ServiceStatus,
        ServiceHold,
        ServiceResume,
        Ignored
    }

    internal sealed class FuelRequest
    {
        public decimal Amount;
        public string Unit;
        public string RawAmountText;
    }

    internal sealed class RampCommand
    {
        public RampCommandType Type;
        public string RawPhrase;
        public string NormalizedPhrase;
        public MatchQuality Quality;
        public string Reason;
        public string[] MenuPatterns = new string[0];
        public PushbackDirection PushDirection;
        public FuelRequest FuelRequest;
        public bool RequiresStrongMatch = true;
        public bool OpensPushbackSubmenu;
        public bool UsesExistingPushbackSubmenu;

        public bool IsActionableGsx
        {
            get
            {
                return MenuPatterns != null && MenuPatterns.Length > 0;
            }
        }

        public bool IsSafeToExecute
        {
            get
            {
                return Quality == MatchQuality.Strong || Quality == MatchQuality.Medium;
            }
        }
    }
}
