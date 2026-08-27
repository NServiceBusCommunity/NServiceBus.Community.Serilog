namespace NServiceBus.Serilog;

/// <summary>
/// Bounds applied to message bodies and saga entities captured as Serilog log event properties.
/// </summary>
/// <remarks>
/// <para>
/// Message bodies and saga entities are captured with destructuring enabled, so the full object graph
/// is written into the log event. An unbounded collection or an oversized string on one of those objects
/// can push the resulting event past the size limit of a sink. Sinks reject an oversized event whole, so
/// a single large property discards the entire audit record, including the message and saga identifiers
/// needed to diagnose it.
/// </para>
/// <para>
/// These limits clip the captured payload so the rest of the event survives. Clipping is never silent:
/// clipped values carry an inline marker, and any event containing a clipped value has a
/// <c>SerilogTruncated</c> property set to <c>true</c> so it can be queried and alerted on.
/// </para>
/// <para>
/// The limits are applied after Serilog has captured the value, so they compose with, and never override,
/// destructuring configured on the logger itself. Capping capture at the source with
/// <c>Destructure.ToMaximumCollectionCount</c> avoids the cost of building the discarded portion of the
/// graph, and is worth doing in addition to these limits on hot paths.
/// </para>
/// </remarks>
public class CaptureLimits
{
    /// <summary>
    /// The limits used when none are configured.
    /// </summary>
    /// <remarks>
    /// Keeps the first 100 items of any collection, clips strings at 8192 characters, and emits at most
    /// 1000 nodes per captured payload.
    /// </remarks>
    public static CaptureLimits Default { get; } = new(
        maxCollectionCount: 100,
        maxStringLength: 8192,
        maxNodes: 1000);

    /// <summary>
    /// Applies no limits, capturing payloads in full.
    /// </summary>
    /// <remarks>
    /// Restores the behaviour of versions prior to the introduction of <see cref="CaptureLimits"/>. An
    /// endpoint using this is responsible for ensuring no message or saga entity can grow large enough
    /// to be rejected by its sinks.
    /// </remarks>
    public static CaptureLimits None { get; } = new(
        maxCollectionCount: int.MaxValue,
        maxStringLength: int.MaxValue,
        maxNodes: int.MaxValue);

    /// <summary>
    /// The maximum number of items kept from any one collection or dictionary.
    /// </summary>
    public int MaxCollectionCount { get; }

    /// <summary>
    /// The maximum number of characters kept from any one string value.
    /// </summary>
    public int MaxStringLength { get; }

    /// <summary>
    /// The maximum number of values captured from a single payload, counting every scalar, collection,
    /// dictionary, and object in the graph.
    /// </summary>
    /// <remarks>
    /// Bounds payloads that stay under <see cref="MaxCollectionCount"/> at every level but are wide
    /// and deep enough to be large overall. Truncation markers are added on top of this budget, so a
    /// clipped payload emits slightly more values than the budget allows.
    /// </remarks>
    public int MaxNodes { get; }

    /// <summary>
    /// Initializes a new <see cref="CaptureLimits"/>.
    /// </summary>
    /// <param name="maxCollectionCount">The maximum number of items kept from any one collection or dictionary.</param>
    /// <param name="maxStringLength">The maximum number of characters kept from any one string value.</param>
    /// <param name="maxNodes">The maximum number of values emitted for a single captured payload.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if any argument is less than one.</exception>
    public CaptureLimits(
        int maxCollectionCount = 100,
        int maxStringLength = 8192,
        int maxNodes = 1000)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCollectionCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxStringLength, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxNodes, 1);

        MaxCollectionCount = maxCollectionCount;
        MaxStringLength = maxStringLength;
        MaxNodes = maxNodes;
    }

    internal bool IsUnlimited =>
        MaxCollectionCount == int.MaxValue &&
        MaxStringLength == int.MaxValue &&
        MaxNodes == int.MaxValue;
}
