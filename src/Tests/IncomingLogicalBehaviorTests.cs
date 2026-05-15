public class IncomingLogicalBehaviorTests
{
    [Test]
    public async Task Simple()
    {
        var logBuilder = new LogBuilder(new FakeLogger(), "endpoint");
        var behavior = new IncomingLogicalBehavior(logBuilder);
        var context = BuildContext();
        Recording.Start();
        await behavior.Inner(context, TestExtensions.WriteLog);
        await Verify(context);
    }

    static TestableIncomingLogicalMessageContext BuildContext() =>
        new()
        {
            Message = new(new(typeof(Message1)), new Message1())
        };

    [Test]
    public async Task WithHeaders()
    {
        var logBuilder = new LogBuilder(new FakeLogger(), "endpoint");
        var behavior = new IncomingLogicalBehavior(logBuilder);
        var context = BuildContext();
        context.MessageHeaders.Add(Headers.ConversationId, Guid.NewGuid().ToString());
        context.MessageHeaders.Add(Headers.CorrelationId, Guid.NewGuid().ToString());
        Recording.Start();
        await behavior.Inner(context, TestExtensions.WriteLog);
        await Verify(context);
    }

    [Test]
    public async Task InvokeThrowsWhenLoggerMissingFromContext()
    {
        var logBuilder = new LogBuilder(new FakeLogger(), "endpoint");
        var behavior = new IncomingLogicalBehavior(logBuilder);
        var context = BuildContext();

        var exception = Assert.Throws<InvalidOperationException>(
            () => behavior.Invoke(context, () => Task.CompletedTask).GetAwaiter().GetResult());

        await Assert.That(exception!.Message).Contains(nameof(IncomingPhysicalBehavior));
    }

    class Message1;
}
