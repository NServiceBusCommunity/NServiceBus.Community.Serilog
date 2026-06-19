// ReSharper disable UnusedVariable

class TracingUsage
{
    TracingUsage()
    {
        var tracingLog = SerilogTracingLogger();

        var configuration = new EndpointConfiguration("EndpointName");

        #region SerilogTracingPassLoggerToFeature

        var serilogTracing = configuration.EnableSerilogTracing(tracingLog);
        serilogTracing.EnableMessageTracing();

        #endregion
    }

    static Logger SerilogTracingLogger()
    {
        #region SerilogTracingLogger

        var configuration = new LoggerConfiguration();
        configuration.Enrich.WithNsbExceptionDetails();
        configuration.WriteTo.File("log.txt");
        configuration.MinimumLevel.Information();
        var tracingLog = configuration.CreateLogger();

        #endregion

        return tracingLog;
    }

    static void EnableSagaTracing(EndpointConfiguration configuration, ILogger logger)
    {
        #region EnableSagaTracing

        var serilogTracing = configuration.EnableSerilogTracing(logger);
        serilogTracing.EnableSagaTracing();

        #endregion
    }

    static void EnableMessageTracing(EndpointConfiguration configuration, ILogger logger)
    {
        #region EnableMessageTracing

        var serilogTracing = configuration.EnableSerilogTracing(logger);
        serilogTracing.EnableMessageTracing();

        #endregion
    }

    static void Seq()
    {
        #region SerilogTracingSeq

        var configuration = new LoggerConfiguration();
        configuration.Enrich.WithNsbExceptionDetails();
        configuration.WriteTo.Seq("http://localhost:5341");
        configuration.MinimumLevel.Information();
        var tracingLog = configuration.CreateLogger();

        #endregion
    }
}

public class TheMessage;
