namespace NServiceBus.Serilog;

/// <summary>
/// All settings for Serilog Tracing.
/// </summary>
public class SerilogTracingSettings
{
    internal ILogger Logger;
    EndpointConfiguration configuration;
    internal ConvertHeader convertHeader = (_, _) => null;
    internal CaptureLimits captureLimits = CaptureLimits.Default;

    internal SerilogTracingSettings(ILogger logger, EndpointConfiguration configuration)
    {
        Logger = logger;
        this.configuration = configuration;
    }

    /// <summary>
    /// Enable tracing of saga state. Measure the performance impact of this setting on the system.
    /// </summary>
    public void EnableSagaTracing() =>
        configuration.EnableFeature<SagaTracingFeature>();

    /// <summary>
    /// Allow a custom log property to be used to a specific header.
    /// </summary>
    public void UseHeaderConversion(ConvertHeader convertHeader) =>
        this.convertHeader = convertHeader;

    /// <summary>
    /// Enable tracing of messages. Measure the performance impact of this setting on the system.
    /// </summary>
    public void EnableMessageTracing() =>
        configuration.EnableFeature<MessageTracingFeature>();

    /// <summary>
    /// Bound the size of message bodies and saga entities captured as log event properties.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="CaptureLimits.Default"/>. Pass <see cref="CaptureLimits.None"/> to capture
    /// payloads in full. See <see cref="CaptureLimits"/> for why the limits exist.
    /// </remarks>
    public void LimitCapture(CaptureLimits limits) =>
        captureLimits = limits;
}