using System;
using System.IO;

namespace SimpleOps.GsxRamp
{
    internal sealed class AppPaths
    {
        public string RootDirectory;
        public string SettingsPath;
        public string PhraseAliasPath;
        public string LogDirectory;
        public string VoiceCacheDirectory;

        public static AppPaths Create()
        {
            var preferredRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SimpleOps");
            return CreateWithFallback(preferredRoot);
        }

        private static AppPaths CreateWithFallback(string preferredRoot)
        {
            try
            {
                return Initialize(preferredRoot);
            }
            catch (UnauthorizedAccessException)
            {
                var localRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appdata");
                return Initialize(localRoot);
            }
            catch (System.Security.SecurityException)
            {
                var localRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appdata");
                return Initialize(localRoot);
            }
        }

        private static AppPaths Initialize(string root)
        {
            var paths = new AppPaths
            {
                RootDirectory = root,
                SettingsPath = Path.Combine(root, "settings.json"),
                PhraseAliasPath = Path.Combine(root, "phrases.json"),
                LogDirectory = Path.Combine(root, "logs"),
                VoiceCacheDirectory = Path.Combine(root, "voice-cache")
            };

            Directory.CreateDirectory(paths.RootDirectory);
            Directory.CreateDirectory(paths.LogDirectory);
            Directory.CreateDirectory(paths.VoiceCacheDirectory);
            return paths;
        }
    }
}
