static Logger ConfigureSerilog()
{
    #region ConfigureSerilog

    var configuration = new LoggerConfiguration();
    configuration.Enrich.WithNsbExceptionDetails();
    configuration.WriteTo.Seq("http://localhost:5341");
    configuration.MinimumLevel.Information();
    var logger = configuration.CreateLogger();

    #endregion

    return logger;
}

Console.Title = "SeqSample";
var tracingLog = ConfigureSerilog();

#region UseConfig

var configuration = new EndpointConfiguration("SeqSample");
var serilogTracing = configuration.EnableSerilogTracing(tracingLog);
serilogTracing.EnableSagaTracing();
serilogTracing.EnableMessageTracing();

#endregion

configuration.UsePersistence<LearningPersistence>();
configuration.UseSerialization<SystemJsonSerializer>();
configuration.UseTransport<LearningTransport>();

var settings = configuration.GetSettings();
settings.Set("NServiceBus.Features.LicenseReminder", FeatureState.Deactivated);

var builder = Host.CreateApplicationBuilder();
// Route NServiceBus log output to the same Serilog logger
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
Console.WriteLine("Message sent");
Console.WriteLine("Press any key to exit");
Console.ReadKey();

#region Cleanup

await host.StopAsync();
Log.CloseAndFlush();

#endregion
