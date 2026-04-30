using System;
using System.Collections.Generic;

namespace SimpleOps.GsxRamp
{
    internal sealed class DisabledSpeechInputService : ISpeechInputService
    {
        public DisabledSpeechInputService(string statusText)
        {
            StatusText = statusText;
        }

        public string StatusText { get; private set; }

        public float LastInputLevel
        {
            get { return 0f; }
        }

        public void Start(IEnumerable<string> phrases, Action<RecognizedPhrase> onRecognized)
        {
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
        }
    }
}
