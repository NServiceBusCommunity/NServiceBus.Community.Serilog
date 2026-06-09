using Microsoft.Extensions.Logging;

// Builds and starts an NServiceBus endpoint inside a Microsoft generic host.
// NServiceBus log output is routed to Serilog (Log.Logger) so Verify's Recording can capture it.
static class EndpointHost
{
    public static async Task<RunningHost> Start(
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
        return new(host);
    }
}

// Stops the host gracefully before disposing it, so in-flight messages drain and the
// transport pump shuts down cleanly. IHost.Dispose alone does not call StopAsync.
sealed class RunningHost(IHost host) :
    IAsyncDisposable
{
    public IServiceProvider Services => host.Services;

    public async ValueTask DisposeAsync()
    {
        await host.StopAsync();
        if (host is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            host.Dispose();
        }
    }
}
