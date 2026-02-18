public class WriteStartupDiagnosticsTests
{
    [Test]
    public Task Can_extract_settings()
    {
        var settings = new SettingsHolder();
        var diagnosticEntries = new StartupDiagnosticEntries();
        diagnosticEntries.Add("Name", "Value");
        settings.Set(diagnosticEntries);
        return Verify(settings.ReadStartupDiagnosticEntries());
    }

    [Test]
    public async Task CleanEntry()
    {
        await Assert.That(StartupDiagnostics.CleanEntry("NServiceBus.Persistence.Sql.SqlDialect")).IsEqualTo("Persistence.Sql.SqlDialect");
        await Assert.That(StartupDiagnostics.CleanEntry("NServiceBus.Transport.SqlServer.CircuitBreaker")).IsEqualTo("Transport.SqlServer.CircuitBreaker");
        await Assert.That(StartupDiagnostics.CleanEntry("Foo")).IsEqualTo("Foo");
    }
}
