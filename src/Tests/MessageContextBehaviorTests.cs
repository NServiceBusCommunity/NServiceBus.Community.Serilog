public class MessageContextBehaviorTests
{
    [Test]
    public async Task SetsIncomingMessageOnExistingState()
    {
        var state = new ExceptionLogState(
            processingEndpoint: new("ProcessingEndpoint", new ScalarValue("endpoint")),
            incomingHeaders: new Dictionary<string, string>(),
            correlationId: null,
            conversationId: null);
        var message = new Message1();
        var context = BuildContext(message);
        context.Extensions.Set(state);

        var behavior = new MessageContextBehavior();
        await behavior.Invoke(context, () => Task.CompletedTask);

        await Assert.That(state.IncomingMessage).IsSameReferenceAs(message);
    }

    [Test]
    public async Task ThrowsWhenStateMissingFromContext()
    {
        var context = BuildContext(new Message1());

        var behavior = new MessageContextBehavior();
        var exception = Assert.Throws<InvalidOperationException>(
            () => behavior.Invoke(context, () => Task.CompletedTask).GetAwaiter().GetResult());

        await Assert.That(exception!.Message).Contains(nameof(IncomingPhysicalBehavior));
    }

    static TestableIncomingLogicalMessageContext BuildContext(object message) =>
        new()
        {
            Message = new(new(message.GetType()), message)
        };

    class Message1;
}
