public class LoggerTests
{
    [Test]
    public async Task DebugFormatRendersIndexedPlaceholders()
    {
        var captured = new CapturingLogger();
        var log = new NServiceBus.Serilog.Logger(captured);

        log.DebugFormat("Hello {0} world {1}", "abc", 42);

        await Assert.That(captured.Events).HasSingleItem();
        await Assert.That(captured.Events[0].MessageTemplate.Text).IsEqualTo("Hello abc world 42");
    }

    [Test]
    public async Task InfoFormatHandlesFormatSpecifiers()
    {
        var captured = new CapturingLogger();
        var log = new NServiceBus.Serilog.Logger(captured);

        log.InfoFormat("Elapsed {0:N3}s", 1.23456);

        await Assert.That(captured.Events).HasSingleItem();
        await Assert.That(captured.Events[0].MessageTemplate.Text).Contains("1.235");
    }

    [Test]
    public async Task FormatWithoutArgsPassesThrough()
    {
        var captured = new CapturingLogger();
        var log = new NServiceBus.Serilog.Logger(captured);

        log.WarnFormat("no placeholders");

        await Assert.That(captured.Events[0].MessageTemplate.Text).IsEqualTo("no placeholders");
    }
}
