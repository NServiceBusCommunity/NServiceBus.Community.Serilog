using TypeNameConverter = NServiceBus.Serilog.TypeNameConverter;

[NotInParallel]
public class IntegrationTests
{
    static IntegrationTests() =>
        LogManager.Use<SerilogFactory>();

    [Test]
    public async Task Handler()
    {
        Recording.Start();
        Recording.Pause();
        await Send(
            new StartHandler
            {
                Property = "TheProperty"
            });
        await Verify();
    }

    [Test]
    public async Task GenericHandler()
    {
        Recording.Start();
        Recording.Pause();
        await Send(
            new StartGenericHandler<string>
            {
                Property = "TheProperty"
            });
        await Verify();
    }

    [Test]
    public async Task WithCustomHeader()
    {
        Recording.Start();
        Recording.Pause();
        await Send(
            new StartHandler
            {
                Property = "TheProperty"
            },
            options => options.SetHeader("CustomHeader", "CustomValue"));
        await Verify();
    }

    [Test]
    public async Task WithConvertedCustomHeader()
    {
        Recording.Start();
        Recording.Pause();
        await Send(
            new StartHandler
            {
                Property = "TheProperty"
            },
            options => options.SetHeader("ConvertHeader", "CustomValue"));
        await Verify();
    }

    //[Fact]
    //public async Task SagaNotFound()
    //{
    //    var events = await Send(
    //        new NotFoundSagaMessage(),
    //        options =>
    //        {
    //            options.SetHeader(Headers.SagaId, Guid.NewGuid().ToString());
    //            options.SetHeader(Headers.SagaType, typeof(NotFoundSaga).FullName);
    //        });
    //    await Verify<NotFoundSagaMessage>(events);
    //}

    [Test]
    public async Task HandlerThatLogs()
    {
        Recording.Start();
        Recording.Pause();
        await Send(new StartHandlerThatLogs());
        await Verify();
    }

    [Test]
    public async Task HandlerThatThrows()
    {
        Recording.Start();
        Recording.Pause();
        await Send(
            new StartHandlerThatThrows
            {
                Property = "TheProperty"
            });
        await Verify();
    }

#if DEBUG

    [Test]
    public async Task Saga()
    {
        Recording.Start();
        Recording.Pause();
        await Send(
            new StartSaga
            {
                Property = "TheProperty"
            });
        await Verify()
            .ScrubMember("Serilog.SagaStateChange");
    }

#endif

    [Test]
    public async Task BehaviorThatThrows()
    {
        Recording.Start();
        Recording.Pause();
        await Send(
            new StartBehaviorThatThrows
            {
                Property = "TheProperty"
            },
            extraConfiguration: _ => _.EnableFeature<BehaviorThatThrowsFeature>());
        await Verify();
    }

    static async Task Send(
        object message,
        Action<SendOptions>? optionsAction = null,
        Action<EndpointConfiguration>? extraConfiguration = null)
    {
        var suffix = TypeNameConverter
            .GetName(message.GetType())
            .MessageTypeName
            .Replace('<', '_')
            .Replace('>', '_');
        var configuration = ConfigBuilder.BuildDefaultConfig("SerilogTests" + suffix);
        configuration.PurgeOnStartup(true);
        extraConfiguration?.Invoke(configuration);

        var serilogTracing = configuration.EnableSerilogTracing();
        serilogTracing.EnableSagaTracing();
        serilogTracing.UseHeaderConversion((key, _) =>
        {
            if (key == "ConvertHeader")
            {
                return new("NewKey", new ScalarValue("newValue"));
            }

            return null;
        });
        serilogTracing.EnableMessageTracing();
        var resetEvent = new ManualResetEvent(false);
        configuration.RegisterComponents(_ => _.AddSingleton(resetEvent));

        var recoverability = configuration.Recoverability();
        recoverability.Delayed(settings =>
        {
            settings.TimeIncrease(TimeSpan.FromMilliseconds(1));
            settings.NumberOfRetries(1);
        });
        recoverability.Immediate(_ => _.NumberOfRetries(1));

        recoverability.Failed(_ => _
            .OnMessageSentToErrorQueue((_, _) =>
            {
                resetEvent.Set();
                return Task.CompletedTask;
            }));

        var endpoint = await Endpoint.Start(configuration);
        var sendOptions = new SendOptions();
        optionsAction?.Invoke(sendOptions);
        sendOptions.SetMessageId("00000000-0000-0000-0000-000000000001");
        sendOptions.RouteToThisEndpoint();
        Recording.Resume();
        await endpoint.Send(message, sendOptions);
        if (!resetEvent.WaitOne(TimeSpan.FromSeconds(10)))
        {
            throw new("No Set received.");
        }

        Recording.Pause();
        await endpoint.Stop();
    }
}
