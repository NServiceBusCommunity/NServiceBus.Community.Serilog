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
    public void CleanEntry()
    {
        AreEqual("Persistence.Sql.SqlDialect", StartupDiagnostics.CleanEntry("NServiceBus.Persistence.Sql.SqlDialect"));
        AreEqual("Transport.SqlServer.CircuitBreaker", StartupDiagnostics.CleanEntry("NServiceBus.Transport.SqlServer.CircuitBreaker"));
        AreEqual("Foo", StartupDiagnostics.CleanEntry("Foo"));
    }
}