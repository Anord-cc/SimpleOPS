using System;
using System.IO;

namespace SimpleOps.GsxRamp
{
    internal sealed class AppLogger : IDisposable
    {
        private readonly object _sync = new object();
        private readonly string _logDirectory;
        private readonly string _fallbackDirectory;
        private StreamWriter _writer;

        public event Action<string> LineLogged;

        public AppLogger(AppPaths paths)
        {
            _logDirectory = paths.LogDirectory;
            _fallbackDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appdata", "logs");
            try
            {
                Directory.CreateDirectory(_logDirectory);
            }
            catch
            {
                Directory.CreateDirectory(_fallbackDirectory);
            }

            OpenWriter();
        }

        public void Log(string message)
        {
            var line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + (message ?? string.Empty);
            lock (_sync)
            {
                try
                {
                    if (_writer == null)
                    {
                        OpenWriter();
                    }

                    _writer.WriteLine(line);
                    _writer.Flush();
                }
                catch
                {
                }
            }

            var handler = LineLogged;
            if (handler != null)
            {
                handler(line);
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                try
                {
                    _writer?.Dispose();
                }
                catch
                {
                }

                _writer = null;
            }
        }

        private void OpenWriter()
        {
            var fileName = "SimpleOps-" + DateTime.Now.ToString("yyyyMMdd") + ".log";
            try
            {
                var path = Path.Combine(_logDirectory, fileName);
                _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
                _writer.AutoFlush = true;
            }
            catch
            {
                Directory.CreateDirectory(_fallbackDirectory);
                var fallbackPath = Path.Combine(_fallbackDirectory, fileName);
                _writer = new StreamWriter(new FileStream(fallbackPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
                _writer.AutoFlush = true;
            }
        }
    }
}
