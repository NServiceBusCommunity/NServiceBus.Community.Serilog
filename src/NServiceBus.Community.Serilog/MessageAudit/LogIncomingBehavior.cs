class LogIncomingBehavior :
    Behavior<IIncomingLogicalMessageContext>
{
    ConvertHeader convertHeader;
    static MessageTemplate messageTemplate;

    internal LogIncomingBehavior(ConvertHeader convertHeader) =>
        this.convertHeader = convertHeader;

    static LogIncomingBehavior()
    {
        var templateParser = new MessageTemplateParser();
        messageTemplate = templateParser.Parse("Receive message {IncomingMessageType} {IncomingMessageId} ({ElapsedTime:N3}s).");
    }

    public static string Name = $"Serilog{nameof(LogIncomingBehavior)}";

    public class Registration :
        RegisterStep
    {
        public Registration(ConvertHeader convertHeader) :
            base(
                stepId: Name,
                behavior: typeof(LogIncomingBehavior),
                description: "Logs incoming messages",
                factoryMethod: _ => new LogIncomingBehavior(convertHeader))
        {
            InsertBefore("MutateIncomingMessages");
            InsertAfter(IncomingLogicalBehavior.Name);
        }
    }

    public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
    {
        var logger = context.Logger();
        // Must match the level used by SerilogExtensions.WriteInfo below.
        if (!logger.IsEnabled(LogEventLevel.Information))
        {
            await next();
            return;
        }

        var message = context.Message;
        var sagaStateChanges = new SagaStateChangeRecorder();
        context.Extensions.Set(sagaStateChanges);
        var startTime = DateTimeOffset.Now;
        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            await next();
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            var finishTime = startTime + elapsed;
            var properties = new List<LogEventProperty>
            {
                new("StartTime", new ScalarValue(startTime.ToLogString())),
                new("FinishTime", new ScalarValue(finishTime.ToLogString())),
                new("ElapsedTime", new ScalarValue(elapsed.TotalSeconds))
            };

            if (logger.BindProperty("IncomingMessage", message.Instance, out var property))
            {
                properties.Add(property);
            }

            if (sagaStateChanges.Value.Length > 0)
            {
                properties.Add(new("Serilog.SagaStateChange", new ScalarValue(sagaStateChanges.Value)));
            }

            properties.AddRange(HeaderAppender.BuildHeaders(context.Headers, convertHeader));
            logger.WriteInfo(messageTemplate, properties);
        }
    }
}