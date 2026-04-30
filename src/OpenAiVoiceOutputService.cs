// Copyright (c) 2026 Alex Nord. All rights reserved.
// SPDX-FileCopyrightText: 2026 Alex Nord
// SPDX-License-Identifier: LicenseRef-AlexNord-Proprietary-SourceAvailable
// See LICENSE.md for terms. No copying, modification, distribution, commercial use, or AI/ML training except by written permission.
//
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SimpleOps.GsxRamp
{
    internal sealed class OpenAiVoiceOutputService : IVoiceOutputService
    {
        private readonly AppSettings _settings;
        private readonly ICredentialStore _credentialStore;
        private readonly AppPaths _paths;
        private readonly Action<string> _log;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private readonly object _sync = new object();
        private string _pendingMessage;
        private bool _disposed;
        private bool _workerScheduled;

        public OpenAiVoiceOutputService(AppSettings settings, ICredentialStore credentialStore, AppPaths paths, Action<string> log)
        {
            _settings = settings ?? AppSettings.CreateDefault();
            _credentialStore = credentialStore;
            _paths = paths;
            _log = log ?? delegate { };
            StatusText = BuildStatusText();
        }

        public string StatusText { get; private set; }

        public bool IsEnabled
        {
            get { return _settings.OpenAiVoiceEnabled && !string.IsNullOrWhiteSpace(_credentialStore.GetSecret("SimpleOps.OpenAI.ApiKey")); }
        }

        public void SpeakAsync(string message)
        {
            if (!_settings.OpenAiVoiceEnabled)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            lock (_sync)
            {
                _pendingMessage = message.Trim();
                if (_workerScheduled)
                {
                    return;
                }

                _workerScheduled = true;
                ThreadPool.QueueUserWorkItem(WorkerLoop);
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _disposed = true;
                _pendingMessage = null;
            }
        }

        private void WorkerLoop(object state)
        {
            while (true)
            {
                string message;
                lock (_sync)
                {
                    if (_disposed)
                    {
                        _workerScheduled = false;
                        return;
                    }

                    message = _pendingMessage;
                    _pendingMessage = null;
                    if (message == null)
                    {
                        _workerScheduled = false;
                        return;
                    }
                }

                try
                {
                    var apiKey = _credentialStore.GetSecret("SimpleOps.OpenAI.ApiKey");
                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        StatusText = "OpenAI voice disabled: API key missing.";
                        return;
                    }

                    var cachePath = GetCachePath(message);
                    if (!File.Exists(cachePath))
                    {
                        DownloadSpeech(message, apiKey, cachePath);
                    }

                    PlayWave(cachePath);
                    StatusText = "OpenAI voice active: " + _settings.OpenAiVoice + ".";
                }
                catch (Exception ex)
                {
                    StatusText = "OpenAI voice warning: " + ex.Message;
                    _log(StatusText);
                }
            }
        }

        private string BuildStatusText()
        {
            if (!_settings.OpenAiVoiceEnabled)
            {
                return "OpenAI voice disabled in settings.";
            }

            if (string.IsNullOrWhiteSpace(_credentialStore.GetSecret("SimpleOps.OpenAI.ApiKey")))
            {
                return "OpenAI voice disabled: API key missing.";
            }

            return "OpenAI voice ready: " + _settings.OpenAiVoice + ".";
        }

        private string GetCachePath(string message)
        {
            using (var sha = SHA1.Create())
            {
                var input = _settings.OpenAiModel + "|" + _settings.OpenAiVoice + "|" + message;
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                var fileName = BitConverter.ToString(hash).Replace("-", string.Empty) + ".wav";
                return Path.Combine(_paths.VoiceCacheDirectory, fileName);
            }
        }

        private void DownloadSpeech(string message, string apiKey, string outputPath)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://api.openai.com/v1/audio/speech");
            request.Method = "POST";
            request.Timeout = 20000;
            request.ReadWriteTimeout = 20000;
            request.ContentType = "application/json";
            request.Accept = "audio/wav";
            request.Headers["Authorization"] = "Bearer " + apiKey;

            var payload = new
            {
                model = _settings.OpenAiModel,
                voice = _settings.OpenAiVoice,
                input = message,
                instructions = "Speak as a concise professional ramp crew operator over an aviation radio. The voice is AI-generated. Keep each reply short and natural.",
                response_format = "wav"
            };

            var body = Encoding.UTF8.GetBytes(_serializer.Serialize(payload));
            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(body, 0, body.Length);
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var file = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                stream.CopyTo(file);
            }
        }

        private void PlayWave(string filePath)
        {
            var outputDevice = ResolveOutputDevice();
            using (var reader = new AudioFileReader(filePath))
            using (var output = new WasapiOut(outputDevice, AudioClientShareMode.Shared, false, 100))
            {
                var resampler = CreateOutputPipeline(reader);
                output.Init(resampler);
                output.Play();
                while (output.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(50);
                }
            }
        }

        private ISampleProvider CreateOutputPipeline(AudioFileReader reader)
        {
            ISampleProvider provider = reader;
            if (reader.WaveFormat.Channels == 1)
            {
                provider = new MonoToStereoSampleProvider(provider);
            }
            else if (reader.WaveFormat.Channels > 2)
            {
                provider = new MonoToStereoSampleProvider(new StereoToMonoSampleProvider(provider));
            }

            return new RadioChannelSampleProvider(provider, (float)_settings.OutputVolume, _settings.OutputChannel, (float)_settings.OutputPan);
        }

        private MMDevice ResolveOutputDevice()
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                if (!string.IsNullOrWhiteSpace(_settings.SpeakerDeviceId))
                {
                    try
                    {
                        return enumerator.GetDevice(_settings.SpeakerDeviceId);
                    }
                    catch
                    {
                        _log("Configured speaker was not found. Falling back to the default output device.");
                    }
                }

                return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
        }
    }

    internal sealed class RadioChannelSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly float _volume;
        private readonly AudioOutputChannel _channel;
        private readonly float _pan;

        public RadioChannelSampleProvider(ISampleProvider source, float volume, AudioOutputChannel channel, float pan)
        {
            _source = source;
            _volume = volume;
            _channel = channel;
            _pan = pan;
            WaveFormat = source.WaveFormat;
        }

        public WaveFormat WaveFormat { get; private set; }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);
            int channels = Math.Max(1, WaveFormat.Channels);
            for (int i = offset; i < offset + samplesRead; i += channels)
            {
                float leftGain = _volume;
                float rightGain = _volume;

                if (_channel == AudioOutputChannel.Left)
                {
                    rightGain = 0f;
                }
                else if (_channel == AudioOutputChannel.Right)
                {
                    leftGain = 0f;
                }
                else
                {
                    if (_pan < 0f)
                    {
                        rightGain *= 1f + _pan;
                    }
                    else if (_pan > 0f)
                    {
                        leftGain *= 1f - _pan;
                    }
                }

                if (channels >= 2)
                {
                    buffer[i] *= leftGain;
                    buffer[i + 1] *= rightGain;
                    for (int channel = 2; channel < channels; channel++)
                    {
                        buffer[i + channel] *= _volume;
                    }
                }
                else
                {
                    buffer[i] *= _volume;
                }
            }

            return samplesRead;
        }
    }
}
