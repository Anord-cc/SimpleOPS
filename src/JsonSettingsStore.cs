// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
using System;
using System.IO;
using System.Web.Script.Serialization;

namespace SimpleOps.GsxRamp
{
    internal sealed class JsonSettingsStore : ISettingsStore
    {
        private readonly AppPaths _paths;
        private readonly Action<string> _log;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        public JsonSettingsStore(AppPaths paths, Action<string> log)
        {
            _paths = paths;
            _log = log ?? delegate { };
        }

        public AppSettings Load()
        {
            try
            {
                if (!File.Exists(_paths.SettingsPath))
                {
                    var defaults = AppSettings.CreateDefault();
                    Save(defaults);
                    return defaults;
                }

                var json = File.ReadAllText(_paths.SettingsPath);
                var settings = _serializer.Deserialize<AppSettings>(json);
                if (settings == null)
                {
                    throw new InvalidOperationException("settings.json was empty.");
                }

                return settings;
            }
            catch (Exception ex)
            {
                _log("Settings load warning: " + ex.Message + ". Falling back to defaults.");
                var defaults = AppSettings.CreateDefault();
                Save(defaults);
                return defaults;
            }
        }

        public void Save(AppSettings settings)
        {
            var json = _serializer.Serialize(settings ?? AppSettings.CreateDefault());
            File.WriteAllText(_paths.SettingsPath, json);
        }
    }
}
