class CaptureSagaStateBehavior(CaptureLimits limits) :
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
        // Must match the level used by SerilogExtensions.WriteInfo below.
        if (!logger.IsEnabled(LogEventLevel.Information))
        {
            await next();
            return;
        }

        var sagaAudit = new SagaUpdatedMessage();
        context.Extensions.Set(sagaAudit);
        var startTime = DateTimeOffset.Now;
        var startTimestamp = Stopwatch.GetTimestamp();

        await next();

        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
        var finishTime = startTime + elapsed;

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
            new("ElapsedTime", new ScalarValue(elapsed.TotalSeconds)),
            new("IsCompleted", new ScalarValue(isCompleted)),
            new("IsNew", new ScalarValue(isNew))
        };

        AddInitiator(context, messageId, properties);

        var truncated = AddResultingMessages(sagaAudit, logger, properties);

        truncated |= AddEntity(logger, saga, properties);

        if (truncated)
        {
            properties.Add(SerilogExtensions.TruncatedProperty());
        }

        logger.WriteInfo(messageTemplate, properties);
    }

    bool AddEntity(ILogger logger, Saga saga, List<LogEventProperty> properties)
    {
        if (!logger.BindProperty("Entity", saga.Entity, limits, out var sagaEntityProperty, out var truncated))
        {
            return false;
        }

        properties.Add(sagaEntityProperty);
        return truncated;
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

    bool AddResultingMessages(SagaUpdatedMessage sagaAudit, ILogger logger, List<LogEventProperty> properties)
    {
        var resultingMessages = sagaAudit.ResultingMessages;
        if (resultingMessages.Count == 0)
        {
            return false;
        }

        if (!logger.BindProperty("ResultingMessages", resultingMessages, limits, out var resultingMessagesProperty, out var truncated))
        {
            return false;
        }

        properties.Add(resultingMessagesProperty);
        return truncated;
    }

    static void AssignSagaStateChangeCausedByMessage(IInvokeHandlerContext context, bool isNew, bool isCompleted, Guid sagaId)
    {
        var stateChange = "Updated";
        if (isNew)
        {
            stateChange = "New";
        }

        if (isCompleted)
        {
            stateChange = "Completed";
        }

        if (!context.Extensions.TryGet<SagaStateChangeRecorder>(out var recorder))
        {
            recorder = new();
            context.Extensions.Set(recorder);
        }

        recorder.Record(sagaId, stateChange);
    }

    public class Registration :
        RegisterStep
    {
        public Registration(CaptureLimits limits) :
            base(
                stepId: $"Serilog{nameof(CaptureSagaStateBehavior)}",
                behavior: typeof(CaptureSagaStateBehavior),
                description: "Records saga state changes",
                factoryMethod: _ => new CaptureSagaStateBehavior(limits)) =>
            InsertBefore("InvokeSaga");
    }
}
