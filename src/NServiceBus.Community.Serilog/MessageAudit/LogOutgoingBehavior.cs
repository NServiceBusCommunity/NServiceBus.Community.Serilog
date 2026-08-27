class LogOutgoingBehavior :
    Behavior<IOutgoingPhysicalMessageContext>
{
    ConvertHeader convertHeader;
    CaptureLimits limits;
    static MessageTemplate messageTemplate;

    LogOutgoingBehavior(ConvertHeader convertHeader, CaptureLimits limits)
    {
        this.convertHeader = convertHeader;
        this.limits = limits;
    }

    static LogOutgoingBehavior()
    {
        var templateParser = new MessageTemplateParser();
        messageTemplate = templateParser.Parse("Sent message {OutgoingMessageType} {OutgoingMessageId}.");
    }

    public override Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
    {
        var logger = context.Logger();
        if (logger.IsEnabled(LogEventLevel.Information))
        {
            var message = context.Extensions
                .Get<OutgoingLogicalMessage>()
                .Instance;
            LogInfoMessage(context, logger, message);
        }

        return next();
    }

    void LogInfoMessage(IOutgoingPhysicalMessageContext context, ILogger logger, object message)
    {
        var properties = new List<LogEventProperty>();

        if (logger.BindProperty("OutgoingMessage", message, limits, out var messageProperty, out var truncated))
        {
            properties.Add(messageProperty);
            if (truncated)
            {
                properties.Add(SerilogExtensions.TruncatedProperty());
            }
        }

        var addresses = context.UnicastAddresses();
        if (addresses.Count > 0)
        {
            if (addresses.Count == 1)
            {
                properties.Add(new("Route", new ScalarValue(addresses[0])));
            }
            else
            {
                var sequence = new SequenceValue(addresses.Select(_ => new ScalarValue(_)));
                properties.Add(new("Routes", sequence));
            }
        }

        properties.AddRange(HeaderAppender.BuildHeaders(context.Headers, convertHeader));
        logger.WriteInfo(messageTemplate, properties);
    }

    public class Registration(ConvertHeader convertHeader, CaptureLimits limits) :
        RegisterStep(
            stepId: $"Serilog{nameof(LogOutgoingBehavior)}",
            behavior: typeof(LogOutgoingBehavior),
            description: "Logs outgoing messages",
            factoryMethod: _ => new LogOutgoingBehavior(convertHeader, limits));
}
