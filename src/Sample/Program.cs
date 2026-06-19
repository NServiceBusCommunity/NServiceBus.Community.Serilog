static void ConfigureSerilog()
{
    #region ConfigureSerilog

    var configuration = new LoggerConfiguration();
    configuration.Enrich.WithNsbExceptionDetails();
    configuration.WriteTo.Console();
    Log.Logger = configuration.CreateLogger();

    #endregion
}

Console.Title = "SerilogSample";
ConfigureSerilog();

var configuration = new EndpointConfiguration("SerilogSample");

configuration.UseSerialization<SystemJsonSerializer>();
configuration.UsePersistence<LearningPersistence>();
configuration.UseTransport<LearningTransport>();

var settings = configuration.GetSettings();
settings.Set("NServiceBus.Features.LicenseReminder", FeatureState.Deactivated);

#region UseConfig

var builder = Host.CreateApplicationBuilder();
builder.Logging.AddSerilog();
builder.Services.AddNServiceBusEndpoint(configuration);

#endregion

using var host = builder.Build();
await host.StartAsync();
var session = host.Services.GetRequiredService<IMessageSession>();
var message = new MyMessage();
await session.SendLocal(message);
Console.WriteLine("Press any key to exit");
Console.ReadKey();

#region Cleanup

await host.StopAsync();
Log.CloseAndFlush();

#endregion
