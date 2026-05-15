public static class TestExtensions
{
    public static void DisableRetries(this EndpointConfiguration configuration)
    {
        var recoverability = configuration.Recoverability();
        recoverability.Delayed(_ => _.NumberOfRetries(0));
        recoverability.Immediate(_ => _.NumberOfRetries(0));
    }

    public static Task WriteLog()
    {
        Log.Error("LogMessage");
        return Task.CompletedTask;
    }
}
