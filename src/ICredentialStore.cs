// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
namespace SimpleOps.GsxRamp
{
    internal interface ICredentialStore
    {
        string GetSecret(string key);
        void SaveSecret(string key, string secret);
        void DeleteSecret(string key);
    }
}
