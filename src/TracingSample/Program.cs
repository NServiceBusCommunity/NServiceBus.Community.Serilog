var loggerConfiguration = new LoggerConfiguration();
loggerConfiguration.WriteTo.Seq("http://localhost:5341");
loggerConfiguration.WriteTo.File("logFile.txt");
loggerConfiguration.MinimumLevel.Information();
var tracingLog = loggerConfiguration.CreateLogger();

var configuration = new EndpointConfiguration("SeqSample");
var serilogTracing = configuration.EnableSerilogTracing(tracingLog);
serilogTracing.EnableSagaTracing();
serilogTracing.EnableMessageTracing();
configuration.EnableInstallers();
configuration.UsePersistence<NonDurablePersistence>();
configuration.UseSerialization<SystemJsonSerializer>();
configuration.UseTransport<LearningTransport>();
configuration.SendFailedMessagesTo("error");

var settings = configuration.GetSettings();
settings.Set("NServiceBus.Features.LicenseReminder", FeatureState.Deactivated);

var recoverability = configuration.Recoverability();
recoverability.Delayed(_ => _.NumberOfRetries(1));
recoverability.Immediate(_ => _.NumberOfRetries(1));

var builder = Host.CreateApplicationBuilder();
// Route NServiceBus log output to Serilog
builder.Logging.AddSerilog(tracingLog);
builder.Services.AddNServiceBusEndpoint(configuration);
using var host = builder.Build();
await host.StartAsync();
var session = host.Services.GetRequiredService<IMessageSession>();
var createUser = new CreateUser
{
    UserName = "jsmith",
    FamilyName = "Smith",
    GivenNames = "John"
};
await session.SendLocal(createUser);
//  await session.ScheduleEvery(TimeSpan.FromSeconds(1), context => context.SendLocal(createUser));
Console.WriteLine("Press any key to stop program");
Console.Read();
await host.StopAsync();
