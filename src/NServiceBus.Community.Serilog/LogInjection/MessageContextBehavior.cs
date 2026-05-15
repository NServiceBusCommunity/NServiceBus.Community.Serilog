class MessageContextBehavior :
    Behavior<IIncomingLogicalMessageContext>
{
    public class Registration :
        RegisterStep
    {
        public Registration() :
            base(
                stepId: $"Serilog{nameof(MessageContextBehavior)}",
                behavior: typeof(MessageContextBehavior),
                description: nameof(MessageContextBehavior)) =>
            InsertBefore(LogIncomingBehavior.Name);
    }

    public override Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
    {
        if (!context.Extensions.TryGet<ExceptionLogState>(out var state))
        {
            throw new InvalidOperationException(
                $"Expected an {nameof(ExceptionLogState)} in the pipeline context. Ensure {nameof(IncomingPhysicalBehavior)} is registered before {nameof(MessageContextBehavior)}.");
        }

        state.IncomingMessage = context.Message.Instance;
        return next();
    }
}