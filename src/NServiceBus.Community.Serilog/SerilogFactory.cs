namespace NServiceBus.Serilog;

/// <summary>
/// Configure NServiceBus logging messages to use Serilog.  Use by calling <see cref="LogManager.Use{T}" /> the T is <see cref="SerilogFactory" />.
/// </summary>
public class SerilogFactory :
    LoggingFactoryDefinition
{
    ILogger? loggerToUse;

    /// <summary>
    /// Creates a new <see cref="SerilogFactory"/>. Freezes <see cref="HeaderAppender"/>'s
    /// exclude set — any subsequent call to <see cref="HeaderAppender.Exclude(string)"/> will throw.
    /// </summary>
    public SerilogFactory() =>
        HeaderAppender.Freeze();

    /// <summary>
    /// <see cref="LoggingFactoryDefinition.GetLoggingFactory" />.
    /// </summary>
    protected override ILoggerFactory GetLoggingFactory() =>
        new LoggerFactory(loggerToUse ?? Log.Logger);

    /// <summary>
    /// Specify an instance of <see cref="ILogger" /> to use. If not specified then the default is Log.Logger.
    /// </summary>
    public void WithLogger(ILogger logger) =>
        loggerToUse = logger;
}