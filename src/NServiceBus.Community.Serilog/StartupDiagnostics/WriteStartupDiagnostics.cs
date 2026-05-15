#region WriteStartupDiagnostics

class StartupDiagnostics(IReadOnlySettings settings, ILogger logger) :
    FeatureStartupTask
{
    readonly ILogger startupLogger = logger.ForContext<StartupDiagnostics>();

    protected override Task OnStart(IMessageSession session, Cancel cancel = default)
    {
        var properties = BuildProperties(settings, startupLogger);

        var templateParser = new MessageTemplateParser();
        var messageTemplate = templateParser.Parse("DiagnosticEntries");
        var logEvent = new LogEvent(
            timestamp: DateTimeOffset.Now,
            level: LogEventLevel.Warning,
            exception: null,
            messageTemplate: messageTemplate,
            properties: properties);
        startupLogger.Write(logEvent);
        return Task.CompletedTask;
    }

    static IEnumerable<LogEventProperty> BuildProperties(
        IReadOnlySettings settings,
        ILogger logger)
    {
        var entries = settings.ReadStartupDiagnosticEntries();
        foreach (var entry in entries)
        {
            if (entry.Name == "Features")
            {
                continue;
            }

            var name = CleanEntry(entry.Name);
            if (logger.BindProperty(name, entry.Data, out var property))
            {
                yield return property;
            }
        }
    }

    internal static string CleanEntry(string entry)
    {
        if (entry.StartsWith("NServiceBus."))
        {
            return entry[12..];
        }

        return entry;
    }

    protected override Task OnStop(IMessageSession session, Cancel cancel = default) =>
        Task.CompletedTask;
}

#endregion
