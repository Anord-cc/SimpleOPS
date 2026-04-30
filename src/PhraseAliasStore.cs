// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace SimpleOps.GsxRamp
{
    internal sealed class PhraseAliasStore
    {
        private readonly AppPaths _paths;
        private readonly Action<string> _log;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public PhraseAliasStore(AppPaths paths, Action<string> log)
        {
            _paths = paths;
            _log = log ?? delegate { };
        }

        public IDictionary<string, string[]> Load()
        {
            try
            {
                if (!File.Exists(_paths.PhraseAliasPath))
                {
                    File.WriteAllText(_paths.PhraseAliasPath, "{}");
                    return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                }

                var json = File.ReadAllText(_paths.PhraseAliasPath);
                var payload = _serializer.Deserialize<Dictionary<string, string[]>>(json);
                if (payload == null)
                {
                    return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                }

                return payload
                    .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                    .ToDictionary(
                        pair => pair.Key,
                        pair => (pair.Value ?? new string[0]).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray(),
                        StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _log("Phrase alias load warning: " + ex.Message + ". Using built-in phrases only.");
                return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
