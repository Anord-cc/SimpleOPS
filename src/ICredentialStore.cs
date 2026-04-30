namespace SimpleOps.GsxRamp
{
    internal interface ICredentialStore
    {
        string GetSecret(string key);
        void SaveSecret(string key, string secret);
        void DeleteSecret(string key);
    }
}
