class CaptureSagaStateBehavior :
    Behavior<IInvokeHandlerContext>
{
    static MessageTemplate messageTemplate;

    static CaptureSagaStateBehavior()
    {
        var templateParser = new MessageTemplateParser();
        messageTemplate = templateParser.Parse("Saga execution {SagaType} {SagaId} ({ElapsedTime:N3}s).");
    }

    public override async Task Invoke(IInvokeHandlerContext context, Func<Task> next)
    {
        if (context.MessageHandler.Instance is not Saga)
        {
            // Message was not handled by the saga
            await next();
            return;
        }

        var logger = context.Logger();
        if (!logger.IsEnabled(LogEventLevel.Information))
        {
            await next();
            return;
        }

        var sagaAudit = new SagaUpdatedMessage();
        context.Extensions.Set(sagaAudit);
        var startTime = DateTimeOffset.Now;

        await next();

        var finishTime = DateTimeOffset.Now;

        if (!context.Extensions.TryGet(out ActiveSagaInstance? activeSagaInstance))
        {
            return;
        }

        var saga = activeSagaInstance.Instance;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (saga.Entity is null)
        {
            //this can happen if it is a timeout or for invoking "saga not found" logic
            return;
        }

        var headers = context.Headers;
        if (!headers.TryGetValue(Headers.MessageId, out var messageId))
        {
            return;
        }

        var isNew = activeSagaInstance.IsNew;
        var isCompleted = saga.Completed;
        var sagaId = saga.Entity.Id;

        AssignSagaStateChangeCausedByMessage(context, isNew, isCompleted, sagaId);

        var properties = new List<LogEventProperty>
        {
            new("SagaType", new ScalarValue(saga.GetType().Name)),
            new("SagaId", new ScalarValue(sagaId)),
            new("StartTime", new ScalarValue(startTime)),
            new("FinishTime", new ScalarValue(finishTime)),
            new("ElapsedTime", new ScalarValue((finishTime - startTime).TotalSeconds)),
            new("IsCompleted", new ScalarValue(isCompleted)),
            new("IsNew", new ScalarValue(isNew))
        };

        AddInitiator(context, messageId, properties);

        AddResultingMessages(sagaAudit, logger, properties);

        AddEntity(logger, saga, properties);

        logger.WriteInfo(messageTemplate, properties);
    }

    static void AddEntity(ILogger logger, Saga saga, List<LogEventProperty> properties)
    {
        if (!logger.BindProperty("Entity", saga.Entity, out var sagaEntityProperty))
        {
            return;
        }

        properties.Add(sagaEntityProperty);
    }

    static void AddInitiator(IInvokeHandlerContext context, string messageId, List<LogEventProperty> properties)
    {
        var initiator = new Dictionary<ScalarValue, LogEventPropertyValue>
        {
            {
                new("IsSagaTimeout"), new ScalarValue(context.IsTimeoutMessage())
            },
            {
                new("MessageId"), new ScalarValue(messageId)
            },
            {
                new("OriginatingMachine"), new ScalarValue(context.OriginatingMachine())
            },
            {
                new("OriginatingEndpoint"), new ScalarValue(context.OriginatingEndpoint())
            },
            {
                new("MessageType"), new ScalarValue(TypeNameConverter.GetName(context.MessageType())
                    .MessageTypeName)
            },
            {
                new("TimeSent"), new ScalarValue(context.TimeSent().ToLogString())
            },
            {
                new("Intent"), new ScalarValue(context.MessageIntent())
            }
        };
        properties.Add(new("Initiator", new DictionaryValue(initiator)));
    }

    static void AddResultingMessages(SagaUpdatedMessage sagaAudit, ILogger logger, List<LogEventProperty> properties)
    {
        var resultingMessages = sagaAudit.ResultingMessages;
        if (resultingMessages.Count == 0)
        {
            return;
        }

        if (!logger.BindProperty("ResultingMessages", resultingMessages, out var resultingMessagesProperty))
        {
            return;
        }

        properties.Add(resultingMessagesProperty);
    }

    static void AssignSagaStateChangeCausedByMessage(IInvokeHandlerContext context, bool isNew, bool isCompleted, Guid sagaId)
    {
        if (!context.Headers.TryGetValue("NServiceBus.Serilog.SagaStateChange", out var sagaStateChange))
        {
            sagaStateChange = string.Empty;
        }

        var stateChange = "Updated";
        if (isNew)
        {
            stateChange = "New";
        }

        if (isCompleted)
        {
            stateChange = "Completed";
        }

        if (!string.IsNullOrEmpty(sagaStateChange))
        {
            sagaStateChange += ';';
        }

        sagaStateChange += $"{sagaId}:{stateChange}";

        context.Headers["NServiceBus.Serilog.SagaStateChange"] = sagaStateChange;
    }

    public class Registration :
        RegisterStep
    {
        public Registration() :
            base(
                stepId: $"Serilog{nameof(CaptureSagaStateBehavior)}",
                behavior: typeof(CaptureSagaStateBehavior),
                description: "Records saga state changes",
                factoryMethod: _ => new CaptureSagaStateBehavior()) =>
            InsertBefore("InvokeSaga");
    }
}