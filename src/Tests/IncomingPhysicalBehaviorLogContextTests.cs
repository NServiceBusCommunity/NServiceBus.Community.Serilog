[NotInParallel]
public class IncomingPhysicalBehaviorLogContextTests
{
    [Test]
    public async Task PushesCorrelationOntoLogContextForStaticLogCalls()
    {
        var events = new List<LogEvent>();
        var capturingLogger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(new EventSink(events.Add))
            .CreateLogger();

        var previousLog = Log.Logger;
        Log.Logger = capturingLogger;
        try
        {
            var logBuilder = new LogBuilder(new FakeLogger(), "endpoint");
            var behavior = new IncomingPhysicalBehavior(logBuilder, "endpoint");
            var context = new RecordingIncomingPhysicalMessageContext(
                headers:
                [
                    new(Headers.CorrelationId, "the-correlation-id"),
                    new(Headers.ConversationId, "the-conversation-id")
                ]);

            await behavior.Invoke(context, () =>
            {
                Log.Information("inside handler");
                return Task.CompletedTask;
            });

            var captured = events.Single(_ => _.MessageTemplate.Text == "inside handler");
            await Assert.That(((ScalarValue) captured.Properties["IncomingMessageId"]).Value).IsEqualTo(context.MessageId);
            await Assert.That(((ScalarValue) captured.Properties["CorrelationId"]).Value).IsEqualTo("the-correlation-id");
            await Assert.That(((ScalarValue) captured.Properties["ConversationId"]).Value).IsEqualTo("the-conversation-id");
        }
        finally
        {
            Log.Logger = previousLog;
            capturingLogger.Dispose();
        }
    }

    [Test]
    public async Task LogContextScopeIsReleasedAfterNext()
    {
        var events = new List<LogEvent>();
        var capturingLogger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(new EventSink(events.Add))
            .CreateLogger();

        var previousLog = Log.Logger;
        Log.Logger = capturingLogger;
        try
        {
            var logBuilder = new LogBuilder(new FakeLogger(), "endpoint");
            var behavior = new IncomingPhysicalBehavior(logBuilder, "endpoint");
            var context = new RecordingIncomingPhysicalMessageContext();

            await behavior.Invoke(context, () => Task.CompletedTask);

            Log.Information("outside handler");

            var captured = events.Single(_ => _.MessageTemplate.Text == "outside handler");
            await Assert.That(captured.Properties.ContainsKey("IncomingMessageId")).IsFalse();
            await Assert.That(captured.Properties.ContainsKey("CorrelationId")).IsFalse();
            await Assert.That(captured.Properties.ContainsKey("ConversationId")).IsFalse();
        }
        finally
        {
            Log.Logger = previousLog;
            capturingLogger.Dispose();
        }
    }
}
