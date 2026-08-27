namespace NServiceBus.Serilog;

/// <summary>
/// Rebuilds a captured <see cref="LogEventPropertyValue"/> graph with <see cref="CaptureLimits"/> applied.
/// </summary>
/// <remarks>
/// The graph handed to this type has already been materialized by Serilog, so it is finite and acyclic.
/// A single instance tracks one payload and is not reusable.
/// </remarks>
class CaptureTruncator(CaptureLimits limits)
{
    internal const string Marker = "[truncated]";

    static ScalarValue markerValue = new(Marker);

    int remainingNodes = limits.MaxNodes;

    /// <summary>
    /// Whether any value was clipped. Only meaningful after <see cref="Truncate"/> has run.
    /// </summary>
    public bool Truncated { get; private set; }

    public LogEventPropertyValue Truncate(LogEventPropertyValue value)
    {
        if (remainingNodes == 0)
        {
            Truncated = true;
            return markerValue;
        }

        remainingNodes--;

        return value switch
        {
            ScalarValue scalar => TruncateScalar(scalar),
            SequenceValue sequence => TruncateSequence(sequence),
            StructureValue structure => TruncateStructure(structure),
            DictionaryValue dictionary => TruncateDictionary(dictionary),
            // Serilog allows custom LogEventPropertyValue implementations. Their contents are opaque,
            // so pass them through rather than dropping them.
            _ => value
        };
    }

    LogEventPropertyValue TruncateScalar(ScalarValue scalar)
    {
        if (scalar.Value is not string value ||
            value.Length <= limits.MaxStringLength)
        {
            return scalar;
        }

        Truncated = true;
        var dropped = value.Length - limits.MaxStringLength;
        return new ScalarValue($"{value[..limits.MaxStringLength]}{Marker} {dropped} more chars");
    }

    LogEventPropertyValue TruncateSequence(SequenceValue sequence)
    {
        var elements = sequence.Elements;
        var keep = Math.Min(elements.Count, limits.MaxCollectionCount);
        var truncated = new List<LogEventPropertyValue>(keep + 1);

        for (var index = 0; index < keep; index++)
        {
            if (remainingNodes == 0)
            {
                AddDropped(truncated, elements.Count - index);
                return new SequenceValue(truncated);
            }

            truncated.Add(Truncate(elements[index]));
        }

        if (keep < elements.Count)
        {
            AddDropped(truncated, elements.Count - keep);
        }

        return new SequenceValue(truncated);
    }

    LogEventPropertyValue TruncateStructure(StructureValue structure)
    {
        var properties = structure.Properties;
        var truncated = new List<LogEventProperty>(properties.Count);

        foreach (var property in properties)
        {
            if (remainingNodes == 0)
            {
                Truncated = true;
                break;
            }

            truncated.Add(new(property.Name, Truncate(property.Value)));
        }

        return new StructureValue(truncated, structure.TypeTag);
    }

    LogEventPropertyValue TruncateDictionary(DictionaryValue dictionary)
    {
        var elements = dictionary.Elements;
        var keep = Math.Min(elements.Count, limits.MaxCollectionCount);
        var truncated = new List<KeyValuePair<ScalarValue, LogEventPropertyValue>>(keep + 1);

        var kept = 0;
        foreach (var element in elements)
        {
            if (kept == keep ||
                remainingNodes == 0)
            {
                break;
            }

            truncated.Add(new(element.Key, Truncate(element.Value)));
            kept++;
        }

        if (kept < elements.Count)
        {
            Truncated = true;
            truncated.Add(new(markerValue, new ScalarValue($"{elements.Count - kept} more entries")));
        }

        return new DictionaryValue(truncated);
    }

    void AddDropped(List<LogEventPropertyValue> truncated, int dropped)
    {
        Truncated = true;
        truncated.Add(new ScalarValue($"{Marker} {dropped} more items"));
    }
}
