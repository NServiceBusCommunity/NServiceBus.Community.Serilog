public class NotFoundSaga :
    Saga<NotFoundSaga.TheSagaData>,
    IAmStartedByMessages<NotFoundSagaMessage>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TheSagaData> mapper) =>
        mapper
            .MapSaga(s => s.Property)
            .ToMessage<NotFoundSagaMessage>(m => m.Property);

    public class TheSagaData :
        ContainSagaData
    {
        public string? Property { get; set; }
    }

    public Task Handle(NotFoundSagaMessage message, HandlerContext context) =>
        Task.Delay(1100, context.CancellationToken);
}