namespace NServiceBus.Serilog;

/// <summary>
/// Configures which message headers are excluded from being promoted to Serilog log event
/// properties by the message-audit, saga-audit, and exception-enrichment paths.
/// </summary>
public static class HeaderAppender
{
    static bool frozen;

    /// <summary>
    /// Add a header name to the set of headers that should not be promoted to log event
    /// properties.
    /// </summary>
    /// <remarks>
    /// Must be called during application startup, before the endpoint is started.
    /// Once the endpoint starts the exclude set is frozen and
    /// subsequent calls to <see cref="Exclude(string)"/> will throw <see cref="InvalidOperationException"/>.
    /// This makes the exclude set effectively immutable for the lifetime of the endpoint and
    /// avoids races between configuration and the running pipeline.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if called after the endpoint has been started.
    /// </exception>
    public static void Exclude(string name)
    {
        ThrowIfFrozen();
        excludeHeaders = new HashSet<string>(excludeHeaders)
        {
            name
        }.ToFrozenSet();
    }

    /// <summary>
    /// Add multiple header names to the set of headers that should not be promoted to
    /// log event properties.
    /// </summary>
    /// <remarks>
    /// Same lifecycle as <see cref="Exclude(string)"/>: must be called during application
    /// startup, before the endpoint is started. Throws once the endpoint has been started.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if called after the endpoint has been started.
    /// </exception>
    public static void Exclude(params string[] names)
    {
        ThrowIfFrozen();
        var updated = new HashSet<string>(excludeHeaders);
        foreach (var name in names)
        {
            updated.Add(name);
        }

        excludeHeaders = updated.ToFrozenSet();
    }

    static void ThrowIfFrozen()
    {
        if (frozen)
        {
            throw new InvalidOperationException(
                $"{nameof(HeaderAppender)}.{nameof(Exclude)} must be called before the endpoint is started. " +
                "The exclude set is frozen once the endpoint has been started.");
        }
    }

    internal static void Freeze() => frozen = true;

    internal static void ResetForTests()
    {
        frozen = false;
        excludeHeaders = FrozenSet.ToFrozenSet(
        [
            Headers.EnclosedMessageTypes,
            Headers.ProcessingEndpoint,
            Headers.CorrelationId,
            Headers.ConversationId,
            Headers.NServiceBusVersion,
            Headers.MessageId
        ]);
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    static FrozenSet<string> excludeHeaders = FrozenSet.ToFrozenSet(
    [
        Headers.EnclosedMessageTypes,
        Headers.ProcessingEndpoint,
        Headers.CorrelationId,
        Headers.ConversationId,
        Headers.NServiceBusVersion,
        Headers.MessageId
    ]);

    internal static IEnumerable<LogEventProperty> BuildHeaders(IReadOnlyDictionary<string, string> headers, ConvertHeader? convertHeader)
    {
        var otherHeaders = new Dictionary<string, string>();
        foreach (var header in headers
                     .Where(_ => !excludeHeaders.Contains(_.Key))
                     .OrderBy(_ => _.Key))
        {
            var key = header.Key;
            var value = header.Value;

            var converted = convertHeader?.Invoke(key, value);
            if (converted != null)
            {
                yield return converted;
                continue;
            }

            if (key.StartsWith("NServiceBus."))
            {
                var name = key[12..];
                if (key == Headers.TimeSent)
                {
                    var dateTime = DateTimeOffsetHelper.ToDateTimeOffset(value);
                    yield return new(name, new ScalarValue(dateTime.ToLogString()));
                    continue;
                }

                if (key == Headers.OriginatingSagaType)
                {
                    value = TypeNameConverter.GetName(value);
                    yield return new(nameof(Headers.OriginatingSagaType), new ScalarValue(value));
                    continue;
                }

                yield return new(name, new ScalarValue(value));
                continue;
            }

            if (key == Headers.OriginatingHostId)
            {
                yield return new(nameof(Headers.OriginatingHostId), new ScalarValue(value));
                continue;
            }

            if (key == Headers.DiagnosticsTraceParent)
            {
                yield return new("TraceParent", new ScalarValue(value));
                continue;
            }

            if (key == Headers.HostDisplayName)
            {
                yield return new(nameof(Headers.HostDisplayName), new ScalarValue(value));
                continue;
            }

            if (key == Headers.HostId)
            {
                yield return new(nameof(Headers.HostId), new ScalarValue(value));
                continue;
            }

            otherHeaders.Add(key, value);
        }

        if (otherHeaders.Count > 0)
        {
            yield return SerilogExtensions.BuildDictionaryProperty("OtherHeaders", otherHeaders);
        }
    }
}
