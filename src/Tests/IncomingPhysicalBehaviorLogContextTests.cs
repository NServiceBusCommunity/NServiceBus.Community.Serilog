public class IncomingPhysicalBehaviorLogContextTests
{
    [Test]
    public async Task PushesCorrelationOntoLogContextForStaticLogCalls()
    {
        var logBuilder = new LogBuilder(new FakeLogger(), "endpoint");
        var behavior = new IncomingPhysicalBehavior(logBuilder, "endpoint");
        var context = new RecordingIncomingPhysicalMessageContext(
            headers:
            [
                new(Headers.CorrelationId, "the-correlation-id"),
                new(Headers.ConversationId, "the-conversation-id")
            ]);

        Recording.Start();
        await behavior.Invoke(context, () =>
        {
            Log.Information("inside handler");
            return Task.CompletedTask;
        });
        await Verify();
    }

    [Test]
    public async Task LogContextScopeIsReleasedAfterNext()
    {
        var logBuilder = new LogBuilder(new FakeLogger(), "endpoint");
        var behavior = new IncomingPhysicalBehavior(logBuilder, "endpoint");
        var context = new RecordingIncomingPhysicalMessageContext();

        Recording.Start();
        await behavior.Invoke(context, () => Task.CompletedTask);

        Log.Information("outside handler");
        await Verify();
    }
}
