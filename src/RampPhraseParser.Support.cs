// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
namespace SimpleOps.GsxRamp
{
    internal sealed partial class RampPhraseParser
    {
        private bool TryParseDeicing(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (ContainsAny(text, "DeicingRequest", "request deicing", "we need deicing", "send the deice truck", "send the deicing truck"))
            {
                Fill(command, RampCommandType.DeicingRequest, MatchQuality.Strong, "Deicing request phrase detected.", "deicing", "deice");
                return true;
            }

            if (ContainsAny(text, "DeicingStart", "start deicing"))
            {
                Fill(command, RampCommandType.DeicingStart, MatchQuality.Strong, "Deicing start phrase detected.", "deicing", "deice");
                return true;
            }

            if (ContainsAny(text, "DeicingTypeOne", "apply type one"))
            {
                Fill(command, RampCommandType.DeicingTypeOne, MatchQuality.Strong, "Type one deicing phrase detected.", "type one", "deicing");
                return true;
            }

            if (ContainsAny(text, "DeicingTypeFour", "apply type four"))
            {
                Fill(command, RampCommandType.DeicingTypeFour, MatchQuality.Strong, "Type four deicing phrase detected.", "type four", "deicing");
                return true;
            }

            if (ContainsAny(text, "DeicingTypeOneAndFour", "type one and type four"))
            {
                Fill(command, RampCommandType.DeicingTypeOneAndFour, MatchQuality.Strong, "Combined deicing phrase detected.", "type one", "type four", "deicing");
                return true;
            }

            if (ContainsAny(text, "DeicingWingsAndTail", "deice wings and tail", "deicing wings and tail"))
            {
                Fill(command, RampCommandType.DeicingWingsAndTail, MatchQuality.Strong, "Deicing wings and tail phrase detected.", "wings and tail", "deicing");
                return true;
            }

            if (ContainsAny(text, "DeicingWings", "deice wings", "deicing wings"))
            {
                Fill(command, RampCommandType.DeicingWings, MatchQuality.Strong, "Deicing wings phrase detected.", "wings", "deicing");
                return true;
            }

            if (ContainsAny(text, "DeicingComplete", "deicing complete"))
            {
                Fill(command, RampCommandType.DeicingComplete, MatchQuality.Strong, "Deicing complete phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            if (ContainsAny(text, "DeicingCancel", "cancel deicing"))
            {
                Fill(command, RampCommandType.DeicingCancel, MatchQuality.Strong, "Deicing cancel phrase detected.", "cancel deicing");
                return true;
            }

            return false;
        }

        private bool TryParseGuidance(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (ContainsAny(text, "FollowMeRequest", "request follow me", "send follow me car", "send follow me", "guide us to the stand", "guide us to the gate"))
            {
                Fill(command, RampCommandType.FollowMeRequest, MatchQuality.Strong, "Follow-me phrase detected.", "follow me");
                return true;
            }

            if (ContainsAny(text, "MarshallerRequest", "request marshaller", "need a marshaller", "marshaller required"))
            {
                Fill(command, RampCommandType.MarshallerRequest, MatchQuality.Strong, "Marshaller request phrase detected.", "marshaller");
                return true;
            }

            if (ContainsAny(text, "MarshallingComplete", "marshalling complete"))
            {
                Fill(command, RampCommandType.MarshallingComplete, MatchQuality.Strong, "Marshalling complete phrase detected.");
                command.RequiresStrongMatch = false;
                return true;
            }

            return false;
        }

        private bool TryParseGenericService(RampCommand command)
        {
            var text = command.NormalizedPhrase;
            if (ContainsAny(text, "ServiceCancel", "cancel service", "cancel current service", "stop current service"))
            {
                Fill(command, RampCommandType.ServiceCancel, MatchQuality.Strong, "Service cancel phrase detected.", "cancel service", "cancel current service");
                return true;
            }

            if (ContainsAny(text, "ServiceStatus", "what is the status", "service status", "what are we waiting for"))
            {
                Fill(command, RampCommandType.ServiceStatus, MatchQuality.Strong, "Service status phrase detected.", "status");
                return true;
            }

            if (ContainsAny(text, "ServiceHold", "hold service"))
            {
                Fill(command, RampCommandType.ServiceHold, MatchQuality.Strong, "Service hold phrase detected.", "hold service");
                return true;
            }

            if (ContainsAny(text, "ServiceResume", "resume service"))
            {
                Fill(command, RampCommandType.ServiceResume, MatchQuality.Strong, "Service resume phrase detected.", "resume service");
                return true;
            }

            return false;
        }
    }
}
