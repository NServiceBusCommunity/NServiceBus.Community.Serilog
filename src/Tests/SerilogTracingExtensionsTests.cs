public class SerilogTracingExtensionsTests
{
    [Test]
    public async Task LoggerReturnsOutgoingLoggerWhenSet()
    {
        var outgoing = new FakeLogger();
        var context = new TestablePipelineContext();
        context.Extensions.Set("SerilogOutgoingLogger", (ILogger) outgoing);

        await Assert.That(context.Logger()).IsSameReferenceAs(outgoing);
    }

    [Test]
    public async Task LoggerReturnsHandlerLoggerWhenOutgoingMissing()
    {
        var handler = new FakeLogger();
        var context = new TestablePipelineContext();
        context.Extensions.Set("SerilogHandlerLogger", (ILogger) handler);

        await Assert.That(context.Logger()).IsSameReferenceAs(handler);
    }

    [Test]
    public async Task LoggerReturnsTypedLoggerWhenNamedKeysMissing()
    {
        var fallback = new FakeLogger();
        var context = new TestablePipelineContext();
        context.Extensions.Set<ILogger>(fallback);

        await Assert.That(context.Logger()).IsSameReferenceAs(fallback);
    }

    [Test]
    public async Task LoggerFallsBackToStaticLogForTestableHandlerContext()
    {
        var context = new TestableMessageHandlerContext();

        await Assert.That(context.Logger()).IsSameReferenceAs(Log.Logger);
    }

    [Test]
    public async Task LoggerThrowsWhenNoneFoundAndNotTestable()
    {
        var context = new TestablePipelineContext();

        var exception = Assert.Throws<Exception>(() => context.Logger());

        await Assert.That(exception.Message).Contains(nameof(SerilogTracingExtensions.EnableSerilogTracing));
    }
}
