using TypeNameConverter = NServiceBus.Serilog.TypeNameConverter;

public class IntegrationTests
{
    static List<LogEvent> logs;

    static IntegrationTests()
    {
        logs = [];
        var eventSink = new EventSink(logs.Add);

        var configuration = new LoggerConfiguration();
        var enrich = configuration.Enrich;
        enrich.WithExceptionDetails();
        enrich.WithNsbExceptionDetails();
        configuration.MinimumLevel.Verbose();
        configuration.WriteTo.Sink(eventSink);
        Log.Logger = configuration.CreateLogger();
        LogManager.Use<SerilogFactory>();
    }

//#if NETCOREAPP3_1
//    [Fact]
//    public async Task WriteStartupDiagnostics()
//    {
//        var events = await Send(
//            new StartHandler
//            {
//                Property = "TheProperty"
//            });
//        await Verify<StartupDiagnostics>(events);
//    }
//#endif

    [Test]
    public async Task Handler()
    {
        Recording.Start();
        var events = await Send(
            new StartHandler
            {
                Property = "TheProperty"
            });
        await Verify<StartHandler>(events);
    }

    [Test]
    public async Task GenericHandler()
    {
        Recording.Start();
        var events = await Send(
            new StartGenericHandler<string>
            {
                Property = "TheProperty"
            });
        await Verify<StartGenericHandler<string>>(events);
    }

    [Test]
    public async Task WithCustomHeader()
    {
        Recording.Start();
        var events = await Send(
            new StartHandler
            {
                Property = "TheProperty"
            },
            options => options.SetHeader("CustomHeader", "CustomValue"));
        await Verify<StartHandler>(events);
    }

    [Test]
    public async Task WithConvertedCustomHeader()
    {
        Recording.Start();
        var events = await Send(
            new StartHandler
            {
                Property = "TheProperty"
            }, options => options.SetHeader("ConvertHeader", "CustomValue"));
        await Verify<StartHandler>(events);
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
        var events = await Send(new StartHandlerThatLogs());
        await Verify<StartHandlerThatLogs>(events);
    }

    [Test]
    public async Task HandlerThatThrows()
    {
        Recording.Start();
        var events = await Send(
            new StartHandlerThatThrows
            {
                Property = "TheProperty"
            });
        await Verify<StartHandlerThatThrows>(events);
    }

#if Debug

    [Test]
    public async Task Saga()
    {
        var events = await Send(
            new StartSaga
            {
                Property = "TheProperty"
            });
        var logEvents = events.ToList();
        await Verify<StartSaga>(logEvents)
            .ScrubMember("Serilog.SagaStateChange");
    }

#endif

    [Test]
    public async Task BehaviorThatThrows()
    {
        Recording.Start();
        var events = await Send(
            new StartBehaviorThatThrows
            {
                Property = "TheProperty"
            },
            extraConfiguration: _ => _.EnableFeature<BehaviorThatThrowsFeature>());
        var logEvents = events.ToList();
        await Verify<StartBehaviorThatThrows>(logEvents);
    }

    static SettingsTask Verify<T>(IEnumerable<LogEventEx> logEvents)
    {
        var list = logEvents.ToList();
        var logsForTarget = list
            .LogsForType<T>()
            .ToList();
        return Verifier.Verify(
            new
            {
                logsForTarget,
                logsForNsbSerilog = list
                    .LogsForNsbSerilog()
                    .ToList(),
                logsWithExceptions = list
                    .LogsWithExceptions()
                    .ToList()
            });
    }

    static async Task<IEnumerable<LogEventEx>> Send(
        object message,
        Action<SendOptions>? optionsAction = null,
        Action<EndpointConfiguration>? extraConfiguration = null)
    {
        logs.Clear();
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
        await endpoint.Send(message, sendOptions);
        if (!resetEvent.WaitOne(TimeSpan.FromSeconds(10)))
        {
            throw new("No Set received.");
        }

        await endpoint.Stop();

        return logs
            .Where(_ => !_.MessageTemplate.Text.StartsWith("Operation canceled"))
            .Select(_ =>
                new LogEventEx
                (
                    messageTemplate: _.MessageTemplate,
                    level: _.Level,
                    properties: _.Properties,
                    exception: _.Exception
                ));
    }
}
