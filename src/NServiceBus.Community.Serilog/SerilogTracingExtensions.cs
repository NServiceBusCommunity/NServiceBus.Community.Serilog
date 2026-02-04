namespace NServiceBus;

/// <summary>
/// Extensions to enable and configure Serilog Tracing.
/// </summary>
public static partial class SerilogTracingExtensions
{
    /// <summary>
    /// Enable Serilog Tracing for this endpoint using Log.Logger as the logging target.
    /// </summary>
    public static SerilogTracingSettings EnableSerilogTracing(this EndpointConfiguration configuration) =>
        configuration.EnableSerilogTracing(Log.Logger);

    /// <summary>
    /// Take NSB specific info from <see cref="Exception.Data" /> and promotes it to Serilog properties.
    /// </summary>
    public static LoggerEnrichmentConfiguration WithNsbExceptionDetails(this LoggerEnrichmentConfiguration configuration, ConvertHeader? convertHeader = null)
    {
        configuration.With(new ExceptionEnricher(convertHeader));
        return configuration;
    }

    /// <summary>
    /// Enable Serilog Tracing for this endpoint.
    /// </summary>
    public static SerilogTracingSettings EnableSerilogTracing(this EndpointConfiguration configuration, ILogger logger)
    {
        configuration.EnableFeature<TracingFeature>();
        var settings = configuration.GetSettings();
        var serilogTracing = new SerilogTracingSettings(logger, configuration);
        settings.Set(serilogTracing);
        return serilogTracing;
    }

    internal static SerilogTracingSettings TracingSettings(this IReadOnlySettings settings) =>
        settings.Get<SerilogTracingSettings>();

    /// <summary>
    /// Get the current <see cref="ILogger" /> for this context.
    /// </summary>
    public static ILogger Logger(this IPipelineContext context)
    {
        var bag = context.Extensions;
        if (bag.TryGet("SerilogOutgoingLogger", out ILogger? logger))
        {
            return logger;
        }

        if (bag.TryGet("SerilogHandlerLogger", out logger))
        {
            return logger;
        }

        if (bag.TryGet(out logger))
        {
            return logger;
        }

        var type = context.GetType();
        while (true)
        {
            if (type.Name == "TestableMessageHandlerContext")
            {
                context.Extensions.Set(Log.Logger);
                return Log.Logger;
            }

            type = type.BaseType;
            if (type is null)
            {
                break;
            }
        }

        throw new(
            $"""
             Expected to find a `{nameof(ILogger)}` in the pipeline context.
             It is possible NServiceBus.Serilog has not been enabled using a call to `{nameof(SerilogTracingExtensions)}.{nameof(EnableSerilogTracing)}()`.
             """);
    }
}