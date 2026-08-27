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

    [Test]
    public async Task ClipsOversizedMessageAndFlagsTheEvent()
    {
        var captured = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(new DelegatingSink(captured.Add))
            .CreateLogger();

        var context = new TestableIncomingLogicalMessageContext
        {
            Message = new(
                new(typeof(BigMessage)),
                new BigMessage
                {
                    Items = Enumerable.Range(0, 500).ToArray()
                })
        };
        context.Extensions.Set((ILogger) logger);

        var behavior = new LogIncomingBehavior(
            convertHeader: (_, _) => null,
            limits: new(maxCollectionCount: 10));
        await behavior.Invoke(context, () => Task.CompletedTask);

        var logEvent = captured.Single();

        await Assert.That(logEvent.Properties.ContainsKey(SerilogExtensions.TruncatedPropertyName)).IsTrue();

        var message = (StructureValue) logEvent.Properties["IncomingMessage"];
        var items = (SequenceValue) message.Properties.Single(_ => _.Name == nameof(BigMessage.Items)).Value;

        // 10 kept, plus the marker reporting the 490 dropped.
        await Assert.That(items.Elements.Count).IsEqualTo(11);
        await Assert.That((string) ((ScalarValue) items.Elements[10]).Value!)
            .IsEqualTo($"{CaptureTruncator.Marker} 490 more items");

        // The rest of the audit record survives, which is the whole point.
        await Assert.That(logEvent.Properties.ContainsKey("StartTime")).IsTrue();
        await Assert.That(logEvent.Properties.ContainsKey("ElapsedTime")).IsTrue();
    }

    [Test]
    public async Task DoesNotFlagEventWhenNothingIsClipped()
    {
        var captured = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(new DelegatingSink(captured.Add))
            .CreateLogger();

        var context = BuildContext();
        context.Extensions.Set((ILogger) logger);

        await BuildBehavior().Invoke(context, () => Task.CompletedTask);

        await Assert.That(captured.Single().Properties.ContainsKey(SerilogExtensions.TruncatedPropertyName)).IsFalse();
    }

    class BigMessage
    {
        public int[] Items { get; init; } = [];
    }

    static TestableIncomingLogicalMessageContext BuildContext() =>
        new()
        {
            Message = new(new(typeof(Message1)), new Message1())
        };

    static LogIncomingBehavior BuildBehavior() =>
        new(convertHeader: (_, _) => null, limits: CaptureLimits.Default);

    class Message1;
}
