namespace TimeTrek.Infrastructure.Settings;

public sealed class SettingsStoreException : Exception
{
    public SettingsStoreException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SettingsStoreException(string message)
        : base(message)
    {
    }
}
