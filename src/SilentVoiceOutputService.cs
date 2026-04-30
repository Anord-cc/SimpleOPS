namespace SimpleOps.GsxRamp
{
    internal sealed class SilentVoiceOutputService : IVoiceOutputService
    {
        public SilentVoiceOutputService(string statusText)
        {
            StatusText = statusText;
        }

        public string StatusText { get; private set; }

        public bool IsEnabled
        {
            get { return false; }
        }

        public void SpeakAsync(string message)
        {
        }

        public void Dispose()
        {
        }
    }
}
