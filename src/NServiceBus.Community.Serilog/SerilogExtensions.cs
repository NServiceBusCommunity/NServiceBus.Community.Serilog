static class SerilogExtensions
{
    /// <summary>
    /// The property added to any log event that carries a clipped payload.
    /// </summary>
    public const string TruncatedPropertyName = "SerilogTruncated";

    static LogEventProperty truncatedProperty = new(TruncatedPropertyName, new ScalarValue(true));

    /// <summary>
    /// Captures a value with destructuring enabled and the supplied limits applied.
    /// </summary>
    /// <remarks>
    /// The <c>truncated</c> output reports whether the captured value was clipped. Callers must add
    /// <see cref="TruncatedProperty"/> to the event when it is set, so clipping is queryable rather
    /// than silent.
    /// </remarks>
    public static bool BindProperty(
        this ILogger logger,
        string name,
        object value,
        CaptureLimits limits,
        [NotNullWhen(true)] out LogEventProperty? property,
        out bool truncated)
    {
        truncated = false;

        if (!logger.BindProperty(name, value, true, out var bound))
        {
            property = null;
            return false;
        }

        if (limits.IsUnlimited)
        {
            property = bound;
            return true;
        }

        var truncator = new CaptureTruncator(limits);
        var clipped = truncator.Truncate(bound.Value);
        truncated = truncator.Truncated;
        property = truncated ? new(name, clipped) : bound;
        return true;
    }

    /// <summary>
    /// Flags an event as carrying a clipped payload.
    /// </summary>
    public static LogEventProperty TruncatedProperty() => truncatedProperty;

    public static void WriteInfo(
        this ILogger logger,
        MessageTemplate messageTemplate,
        IEnumerable<LogEventProperty> properties)
    {
        var logEvent = new LogEvent(
            timestamp: DateTimeOffset.Now,
            level: LogEventLevel.Information,
            exception: null,
            messageTemplate: messageTemplate,
            properties: properties);
        logger.Write(logEvent);
    }

    public static string ToLogString(this DateTimeOffset date) =>
        date.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffzz");

    public static LogEventProperty BuildDictionaryProperty(string name, IReadOnlyDictionary<string, string> otherHeaders) =>
        new(
            name,
            new DictionaryValue(
                otherHeaders.Select(_ =>
                    new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                        new(_.Key),
                        new ScalarValue(_.Value)))));
}
