// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Speech.AudioFormat;
using System.Speech.Recognition;
using System.Threading;
using NAudio.Wave;

namespace SimpleOps.GsxRamp
{
    internal sealed class LocalSpeechInputService : ISpeechInputService
    {
        private readonly AppSettings _settings;
        private readonly Action<string> _log;

        private readonly object _sync = new object();
        private SpeechRecognitionEngine _recognizer;
        private WaveInEvent _waveIn;
        private BufferedCaptureStream _captureStream;

        public LocalSpeechInputService(AppSettings settings, Action<string> log)
        {
            _settings = settings ?? AppSettings.CreateDefault();
            _log = log ?? delegate { };
            StatusText = "Speech input ready.";
        }

        public string StatusText { get; private set; }

        public float LastInputLevel { get; private set; }

        public void Start(IEnumerable<string> phrases, Action<RecognizedPhrase> onRecognized)
        {
            lock (_sync)
            {
                Stop();

                var phraseList = (phrases ?? Enumerable.Empty<string>()).Where(phrase => !string.IsNullOrWhiteSpace(phrase)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                if (phraseList.Length == 0)
                {
                    StatusText = "Speech input disabled: no phrases loaded.";
                    return;
                }

                var selectedDevice = ResolveInputDevice();
                _captureStream = new BufferedCaptureStream();
                _waveIn = new WaveInEvent
                {
                    DeviceNumber = selectedDevice.Index,
                    BufferMilliseconds = 100,
                    NumberOfBuffers = 3,
                    WaveFormat = new WaveFormat(16000, 16, 1)
                };
                _waveIn.DataAvailable += OnWaveDataAvailable;
                _waveIn.RecordingStopped += OnRecordingStopped;

                var culture = new CultureInfo("en-US");
                _recognizer = new SpeechRecognitionEngine(culture);
                _recognizer.SpeechRecognized += delegate(object sender, SpeechRecognizedEventArgs e)
                {
                    if (e.Result == null)
                    {
                        return;
                    }

                    var handler = onRecognized;
                    if (handler != null)
                    {
                        handler(new RecognizedPhrase
                        {
                            Text = e.Result.Text,
                            Confidence = e.Result.Confidence,
                            AudioLevel = LastInputLevel
                        });
                    }
                };
                _recognizer.SetInputToAudioStream(
                    _captureStream,
                    new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));

                var choices = new Choices();
                choices.Add(phraseList);
                var grammarBuilder = new GrammarBuilder { Culture = culture };
                grammarBuilder.Append(choices);
                var grammar = new Grammar(grammarBuilder) { Name = "SimpleOpsRampGrammar" };
                _recognizer.LoadGrammar(grammar);
                _recognizer.RecognizeAsync(RecognizeMode.Multiple);

                _waveIn.StartRecording();
                StatusText = "Speech input active on " + selectedDevice.Name + ".";
                _log(StatusText);
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                try
                {
                    _waveIn?.StopRecording();
                }
                catch
                {
                }

                try
                {
                    _recognizer?.RecognizeAsyncCancel();
                    _recognizer?.RecognizeAsyncStop();
                }
                catch
                {
                }

                try
                {
                    _recognizer?.Dispose();
                }
                catch
                {
                }

                try
                {
                    _waveIn?.Dispose();
                }
                catch
                {
                }

                try
                {
                    _captureStream?.Dispose();
                }
                catch
                {
                }

                _recognizer = null;
                _waveIn = null;
                _captureStream = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private void OnWaveDataAvailable(object sender, WaveInEventArgs e)
        {
            if (_captureStream != null && e != null && e.BytesRecorded > 0)
            {
                LastInputLevel = ComputeLevel(e.Buffer, e.BytesRecorded);
                _captureStream.AddSamples(e.Buffer, e.BytesRecorded);
            }
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (e != null && e.Exception != null)
            {
                StatusText = "Speech input warning: " + e.Exception.Message;
                _log(StatusText);
            }
        }

        private AudioInputDeviceInfo ResolveInputDevice()
        {
            var devices = AudioDeviceCatalog.GetInputDevices();
            if (devices.Count == 0)
            {
                throw new InvalidOperationException("No microphone devices were found.");
            }

            if (!string.IsNullOrWhiteSpace(_settings.MicrophoneDeviceName))
            {
                var exact = devices.FirstOrDefault(device => string.Equals(device.Name, _settings.MicrophoneDeviceName, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                {
                    return exact;
                }

                _log("Configured microphone '" + _settings.MicrophoneDeviceName + "' was not found. Falling back to default input.");
            }

            return devices[0];
        }

        private static float ComputeLevel(byte[] buffer, int count)
        {
            if (buffer == null || count < 2)
            {
                return 0f;
            }

            double peak = 0d;
            for (int i = 0; i < count - 1; i += 2)
            {
                short sample = BitConverter.ToInt16(buffer, i);
                double level = Math.Abs(sample / 32768d);
                if (level > peak)
                {
                    peak = level;
                }
            }

            return (float)peak;
        }

        private sealed class BufferedCaptureStream : Stream
        {
            private readonly Queue<byte> _buffer = new Queue<byte>();
            private readonly AutoResetEvent _dataAvailable = new AutoResetEvent(false);
            private readonly object _sync = new object();
            private bool _closed;

            public void AddSamples(byte[] data, int count)
            {
                lock (_sync)
                {
                    for (int i = 0; i < count; i++)
                    {
                        _buffer.Enqueue(data[i]);
                    }
                }

                _dataAvailable.Set();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                while (true)
                {
                    lock (_sync)
                    {
                        if (_buffer.Count > 0)
                        {
                            int bytesToRead = Math.Min(count, _buffer.Count);
                            for (int i = 0; i < bytesToRead; i++)
                            {
                                buffer[offset + i] = _buffer.Dequeue();
                            }

                            return bytesToRead;
                        }

                        if (_closed)
                        {
                            return 0;
                        }
                    }

                    _dataAvailable.WaitOne(100);
                }
            }

            protected override void Dispose(bool disposing)
            {
                _closed = true;
                _dataAvailable.Set();
                base.Dispose(disposing);
            }

            public override bool CanRead { get { return true; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { throw new NotSupportedException(); } }
            public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        }
    }
}
