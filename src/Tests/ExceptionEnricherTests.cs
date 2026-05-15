public class ExceptionEnricherTests
{
    [Test]
    public async Task UsesPropertyFactoryDestructuringPolicy()
    {
        var captured = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .Destructure.ByTransforming<SecretMessage>(_ => new { Redacted = true })
            .Enrich.With(new ExceptionEnricher(header: null))
            .WriteTo.Sink(new DelegatingSink(captured.Add))
            .CreateLogger();

        var exception = new InvalidOperationException("boom");
        var logState = new ExceptionLogState(
            processingEndpoint: new("ProcessingEndpoint", new ScalarValue("endpoint")),
            incomingHeaders: new Dictionary<string, string>(),
            correlationId: null,
            conversationId: null)
        {
            IncomingMessage = new SecretMessage("hunter2")
        };
        exception.Data.Add("ExceptionLogState", logState);

        logger.Error(exception, "test");

        await Assert.That(captured).HasSingleItem();
        var message = (StructureValue) captured[0].Properties["IncomingMessage"];
        var redacted = message.Properties.Single(_ => _.Name == "Redacted");
        await Assert.That((bool) ((ScalarValue) redacted.Value).Value!).IsTrue();
    }

    class SecretMessage(string token)
    {
        public string Token => token;
    }

    class DelegatingSink(Action<LogEvent> handler) : ILogEventSink
    {
        public void Emit(LogEvent logEvent) => handler(logEvent);
    }
}
