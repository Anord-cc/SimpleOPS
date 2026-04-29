namespace SimpleOps.GsxRamp
{
    internal sealed partial class RampPhraseParser
    {
        private static bool TryParseDeicing(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (TextUtility.ContainsAny(text, "request deicing", "we need deicing", "send the deicing truck"))
            {
                Fill(command, RampCommandType.DeicingRequest, MatchQuality.Strong, "Deicing request phrase detected.", "deicing", "deice");
                return true;
            }

            if (TextUtility.ContainsAny(text, "start deicing"))
            {
                Fill(command, RampCommandType.DeicingStart, MatchQuality.Strong, "Deicing start phrase detected.", "deicing", "deice");
                return true;
            }

            if (TextUtility.ContainsAny(text, "apply type one"))
            {
                Fill(command, RampCommandType.DeicingTypeOne, MatchQuality.Strong, "Type one deicing phrase detected.", "type one", "deicing");
                return true;
            }

            if (TextUtility.ContainsAny(text, "apply type four"))
            {
                Fill(command, RampCommandType.DeicingTypeFour, MatchQuality.Strong, "Type four deicing phrase detected.", "type four", "deicing");
                return true;
            }

            if (TextUtility.ContainsAny(text, "type one and type four"))
            {
                Fill(command, RampCommandType.DeicingTypeOneAndFour, MatchQuality.Strong, "Combined deicing phrase detected.", "type one", "type four", "deicing");
                return true;
            }

            if (TextUtility.ContainsAny(text, "deicing wings and tail"))
            {
                Fill(command, RampCommandType.DeicingWingsAndTail, MatchQuality.Strong, "Deicing wings and tail phrase detected.", "wings and tail", "deicing");
                return true;
            }

            if (TextUtility.ContainsAny(text, "deicing wings"))
            {
                Fill(command, RampCommandType.DeicingWings, MatchQuality.Strong, "Deicing wings phrase detected.", "wings", "deicing");
                return true;
            }

            if (TextUtility.ContainsAny(text, "deicing complete"))
            {
                Fill(command, RampCommandType.DeicingComplete, MatchQuality.Strong, "Deicing complete phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            if (TextUtility.ContainsAny(text, "cancel deicing"))
            {
                Fill(command, RampCommandType.DeicingCancel, MatchQuality.Strong, "Deicing cancel phrase detected.", "cancel deicing");
                return true;
            }

            return false;
        }

        private static bool TryParseGuidance(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (TextUtility.ContainsAny(text, "request follow me", "send follow me", "guide us to the stand", "guide us to the gate"))
            {
                Fill(command, RampCommandType.FollowMeRequest, MatchQuality.Strong, "Follow-me phrase detected.", "follow me");
                return true;
            }

            if (TextUtility.ContainsAny(text, "request marshaller", "need a marshaller", "marshaller required"))
            {
                Fill(command, RampCommandType.MarshallerRequest, MatchQuality.Strong, "Marshaller request phrase detected.", "marshaller");
                return true;
            }

            if (TextUtility.ContainsAny(text, "marshalling complete"))
            {
                Fill(command, RampCommandType.MarshallingComplete, MatchQuality.Strong, "Marshalling complete phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            return false;
        }

        private static bool TryParseGenericService(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (TextUtility.ContainsAny(text, "cancel service", "cancel current service", "stop current service"))
            {
                Fill(command, RampCommandType.ServiceCancel, MatchQuality.Strong, "Service cancel phrase detected.", "cancel service", "cancel current service");
                return true;
            }

            if (TextUtility.ContainsAny(text, "what is the status", "service status", "what are we waiting for"))
            {
                Fill(command, RampCommandType.ServiceStatus, MatchQuality.Strong, "Service status phrase detected.", "status");
                return true;
            }

            if (TextUtility.ContainsAny(text, "hold service"))
            {
                Fill(command, RampCommandType.ServiceHold, MatchQuality.Strong, "Service hold phrase detected.", "hold service");
                return true;
            }

            if (TextUtility.ContainsAny(text, "resume service"))
            {
                Fill(command, RampCommandType.ServiceResume, MatchQuality.Strong, "Service resume phrase detected.", "resume service");
                return true;
            }

            return false;
        }
    }
}
