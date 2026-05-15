public class LogIncomingBehaviorTests
{
    [Test]
    public async Task EmitsAuditOnSuccess()
    {
        var context = BuildContext();
        context.Extensions.Set(Log.Logger);
        Recording.Start();

        await BuildBehavior().Invoke(context, () => Task.CompletedTask);

        await Verify(context);
    }

    [Test]
    public async Task EmitsAuditWhenNextThrows()
    {
        var context = BuildContext();
        context.Extensions.Set(Log.Logger);
        Recording.Start();

        var caught = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BuildBehavior().Invoke(context, () => throw new InvalidOperationException("boom")));

        await Assert.That(caught!.Message).IsEqualTo("boom");
        await Verify(context);
    }

    [Test]
    public async Task SkipsWhenInformationDisabled()
    {
        var disabledLogger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .CreateLogger();
        var context = BuildContext();
        context.Extensions.Set((ILogger) disabledLogger);
        Recording.Start();

        var nextCalled = false;
        await BuildBehavior().Invoke(context, () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await Assert.That(nextCalled).IsTrue();
        await Verify(context);
    }

    static TestableIncomingLogicalMessageContext BuildContext() =>
        new()
        {
            Message = new(new(typeof(Message1)), new Message1())
        };

    static LogIncomingBehavior BuildBehavior() =>
        new(convertHeader: (_, _) => null);

    class Message1;
}
