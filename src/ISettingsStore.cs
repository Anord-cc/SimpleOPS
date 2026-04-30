namespace SimpleOps.GsxRamp
{
    internal interface ISettingsStore
    {
        AppSettings Load();
        void Save(AppSettings settings);
    }
}
