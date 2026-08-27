public class CaptureTruncatorTests
{
    [Test]
    public async Task LeavesSmallPayloadUntouched()
    {
        var (property, truncated) = Bind(
            new
            {
                Name = "small",
                Items = new[] { 1, 2, 3 }
            },
            new CaptureLimits());

        await Assert.That(truncated).IsFalse();
        await Assert.That(Render(property)).DoesNotContain(CaptureTruncator.Marker);
    }

    [Test]
    public async Task ClipsCollectionAndReportsDroppedCount()
    {
        var (property, truncated) = Bind(
            new
            {
                Items = Enumerable.Range(0, 50).ToArray()
            },
            new CaptureLimits(maxCollectionCount: 10));

        await Assert.That(truncated).IsTrue();

        var items = (SequenceValue) ((StructureValue) property.Value).Properties.Single().Value;
        await Assert.That(items.Elements.Count).IsEqualTo(11);
        await Assert.That(Scalar(items.Elements[10])).IsEqualTo($"{CaptureTruncator.Marker} 40 more items");
    }

    [Test]
    public async Task KeepsTheFirstItemsOfAClippedCollection()
    {
        var (property, _) = Bind(
            new
            {
                Items = Enumerable.Range(0, 50).ToArray()
            },
            new CaptureLimits(maxCollectionCount: 3));

        var items = (SequenceValue) ((StructureValue) property.Value).Properties.Single().Value;
        var kept = items.Elements
            .Take(3)
            .Select(_ => (int) ((ScalarValue) _).Value!);
        await Assert.That(kept).IsEquivalentTo([0, 1, 2]);
    }

    [Test]
    public async Task ClipsLongString()
    {
        var (property, truncated) = Bind(
            new
            {
                Text = new string('x', 100)
            },
            new CaptureLimits(maxStringLength: 10));

        await Assert.That(truncated).IsTrue();

        var text = Scalar(((StructureValue) property.Value).Properties.Single().Value);
        await Assert.That(text).IsEqualTo($"xxxxxxxxxx{CaptureTruncator.Marker} 90 more chars");
    }

    [Test]
    public async Task ClipsDictionary()
    {
        var (property, truncated) = Bind(
            Enumerable.Range(0, 20).ToDictionary(_ => $"key{_}", _ => _),
            new CaptureLimits(maxCollectionCount: 5));

        await Assert.That(truncated).IsTrue();

        var dictionary = (DictionaryValue) property.Value;
        await Assert.That(dictionary.Elements.Count).IsEqualTo(6);
        await Assert.That(Scalar(dictionary.Elements[new(CaptureTruncator.Marker)])).IsEqualTo("15 more entries");
    }

    [Test]
    public async Task NodeBudgetBoundsWideAndDeepPayloads()
    {
        // Every collection stays under MaxCollectionCount, so only the node budget can bound this.
        var (property, truncated) = Bind(
            new
            {
                Outer = Enumerable
                    .Range(0, 5)
                    .Select(_ => new
                    {
                        Inner = Enumerable.Range(0, 5).ToArray()
                    })
                    .ToArray()
            },
            new CaptureLimits(maxCollectionCount: 10, maxNodes: 12));

        await Assert.That(truncated).IsTrue();

        // MaxNodes bounds values captured from the payload. Markers are added on top of that
        // budget so truncation is always visible, so they are excluded from the count.
        var captured = CountNodes(property.Value) - CountMarkers(property.Value);
        await Assert.That(captured).IsLessThanOrEqualTo(12);
    }

    [Test]
    public async Task PreservesTypeTag()
    {
        var (property, _) = Bind(
            new Tagged
            {
                Items = Enumerable.Range(0, 50).ToArray()
            },
            new CaptureLimits(maxCollectionCount: 2));

        await Assert.That(((StructureValue) property.Value).TypeTag).IsEqualTo(nameof(Tagged));
    }

    [Test]
    public async Task NoneCapturesInFull()
    {
        var (property, truncated) = Bind(
            new
            {
                Items = Enumerable.Range(0, 5000).ToArray()
            },
            CaptureLimits.None);

        await Assert.That(truncated).IsFalse();

        var items = (SequenceValue) ((StructureValue) property.Value).Properties.Single().Value;
        await Assert.That(items.Elements.Count).IsEqualTo(5000);
    }

    [Test]
    public async Task DefaultLimitsClipAnUnboundedSagaEntity()
    {
        // Mirrors the failure this guards against: a saga entity accumulating an unbounded
        // collection until the whole event is rejected by the sink.
        var entity = new
        {
            Key = Guid.NewGuid(),
            Entries = Enumerable
                .Range(0, 800)
                .Select(_ => new
                {
                    EntityId = Guid.NewGuid(),
                    EntityType = "Indicator",
                    Action = "Deleted",
                    Title = $"E2E-Indicator-{_}"
                })
                .ToArray()
        };

        var (property, truncated) = Bind(entity, CaptureLimits.Default);

        await Assert.That(truncated).IsTrue();
        await Assert.That(Render(property).Length).IsLessThan(262144);
    }

    [Test]
    public void RejectsNonPositiveLimits()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CaptureLimits(maxCollectionCount: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CaptureLimits(maxStringLength: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CaptureLimits(maxNodes: 0));
    }

    static (LogEventProperty Property, bool Truncated) Bind(object value, CaptureLimits limits)
    {
        var logger = new LoggerConfiguration().CreateLogger();
        if (!logger.BindProperty("Payload", value, limits, out var property, out var truncated))
        {
            throw new("Failed to bind the payload.");
        }

        return (property, truncated);
    }

    static string Scalar(LogEventPropertyValue value) =>
        (string) ((ScalarValue) value).Value!;

    static string Render(LogEventProperty property)
    {
        var writer = new StringWriter();
        property.Value.Render(writer);
        return writer.ToString();
    }

    static int CountMarkers(LogEventPropertyValue value) =>
        value switch
        {
            ScalarValue scalar => scalar.Value is string text && text.Contains(CaptureTruncator.Marker) ? 1 : 0,
            SequenceValue sequence => sequence.Elements.Sum(CountMarkers),
            StructureValue structure => structure.Properties.Sum(_ => CountMarkers(_.Value)),
            DictionaryValue dictionary => dictionary.Elements.Sum(_ => CountMarkers(_.Value)),
            _ => 0
        };

    static int CountNodes(LogEventPropertyValue value) =>
        value switch
        {
            SequenceValue sequence => 1 + sequence.Elements.Sum(CountNodes),
            StructureValue structure => 1 + structure.Properties.Sum(_ => CountNodes(_.Value)),
            DictionaryValue dictionary => 1 + dictionary.Elements.Sum(_ => CountNodes(_.Value)),
            _ => 1
        };

    class Tagged
    {
        public int[] Items { get; init; } = [];
    }
}
