class CaptureSagaResultingBehavior :
    Behavior<IOutgoingLogicalMessageContext>
{
    public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
    {
        AppendMessageToState(context);
        return next();
    }

    static void AppendMessageToState(IOutgoingLogicalMessageContext context)
    {
        if (!context.Extensions.TryGet(out SagaUpdatedMessage? updatedMessage))
        {
            return;
        }

        var logicalMessage = context.Message;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (logicalMessage is null)
        {
            //this can happen on control messages
            return;
        }

        var messageType = logicalMessage.MessageType.Name;

        var resultingMessage = new Dictionary<string, string>
        {
            {
                "Id", context.MessageId
            },
            {
                "Type", messageType
            },
            {
                "Intent", context.MessageIntent()
            }
        };
        var destination = context.GetDestinationForUnicastMessages();
        if (destination is not null)
        {
            resultingMessage.Add("Destination", destination);
        }

        updatedMessage.ResultingMessages.Add(resultingMessage);
    }

    public class Registration() :
        RegisterStep(
            stepId: $"Serilog{nameof(CaptureSagaResultingBehavior)}",
            behavior: typeof(CaptureSagaResultingBehavior),
            description: "Reports messages outgoing from a saga to Serilog",
            factoryMethod: _ => new CaptureSagaResultingBehavior());
}