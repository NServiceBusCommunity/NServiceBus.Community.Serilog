using System.Threading.Tasks;

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

    class Message1;
}