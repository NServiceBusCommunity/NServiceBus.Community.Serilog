public class CreateUserSaga :
    Saga<CreateUserSagaData>,
    IAmStartedByMessages<CreateUser>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CreateUserSagaData> mapper) =>
        mapper
            .MapSaga(sagaData => sagaData.UserName)
            .ToMessage<CreateUser>(message => message.UserName);

    public Task Handle(CreateUser message, HandlerContext context)
    {
        Data.UserName = message.UserName;
        context.LogInformation("User Created {@Message}", message);
        var userCreated = new UserCreated
        {
            UserName = message.UserName
        };
        MarkAsComplete();
        return context.SendLocal(userCreated);
    }
}