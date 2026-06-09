public class WithNoTracingTests
{
    [Test]
    public async Task Handler()
    {
        Exception? exception = null;
        var resetEvent = new ManualResetEvent(false);
        var configuration = ConfigBuilder.BuildDefaultConfig("WithNoTracingTests");
        configuration.DisableRetries();
        var recoverability = configuration.Recoverability();
        recoverability.Failed(_ => _
            .OnMessageSentToErrorQueue((message, _) =>
            {
                exception = message.Exception;
                resetEvent.Set();
                return Task.CompletedTask;
            }));

        await using var host = await EndpointHost.Start(configuration, _ => _.AddSingleton(resetEvent));
        var session = host.Services.GetRequiredService<IMessageSession>();
        await session.SendLocal(new StartHandler());
        if (!resetEvent.WaitOne(TimeSpan.FromSeconds(2)))
        {
            throw new("No Set received.");
        }

        await Verify(exception!.Message);
    }
}
