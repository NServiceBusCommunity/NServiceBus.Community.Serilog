using Microsoft.Extensions.Logging;

// Builds and starts an NServiceBus endpoint inside a Microsoft generic host.
// NServiceBus log output is routed to Serilog (Log.Logger) so Verify's Recording can capture it.
static class EndpointHost
{
    public static async Task<IHost> Start(
        EndpointConfiguration configuration,
        Action<IServiceCollection>? configureServices = null)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
        builder.Logging.AddSerilog();
        configureServices?.Invoke(builder.Services);
        builder.Services.AddNServiceBusEndpoint(configuration);
        var host = builder.Build();
        await host.StartAsync();
        return host;
    }
}
