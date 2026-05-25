public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifySerilog.Initialize(_ =>
        {
            var enrich = _.Enrich;
            enrich.WithExceptionDetails();
            enrich.WithNsbExceptionDetails();
        });
        VerifySerilog.IgnoreSourceContext("NServiceBus.LearningTransportMessagePump");
        VerifySerilog.IgnoreSourceContext("NServiceBus.RoutingToDispatchConnector");
        VerifySerilog.IgnoreSourceContext("NServiceBus.EnvelopeUnwrapper");
        VerifySerilog.IgnoreSourceContext("NServiceBus.LoadHandlersConnector");
        VerifierSettings.InitializePlugins();
        var nsbVersion = FileVersionInfo.GetVersionInfo(typeof(EndpointConfiguration).Assembly.Location);
        var nsbVersionString = $"{nsbVersion.FileMajorPart}.{nsbVersion.FileMinorPart}.{nsbVersion.FileBuildPart}";
        VerifierSettings.IgnoreStackTrace();
        VerifierSettings.AddScrubber(_ => _.Replace(nsbVersionString, "NsbVersion"));
        VerifierSettings.ScrubMachineName();
        VerifierSettings.ScrubInlineGuids();
        VerifierSettings.AddExtraDateTimeOffsetFormat("yyyy-MM-dd HH:mm:ss:ffffff Z");
        VerifierSettings.AddExtraDateTimeOffsetFormat("yyyy-MM-ddTHH:mm:ss.fffzz");
        VerifierSettings
            .ScrubMembers(
                "ElapsedTime",
                "TraceParent",
                "Task",
                "TargetSite",
                "baggage",
                "StartTime",
                "FinishTime",
                "HandlerStartTime",
                "HandlerFailureTime",
                "Handler start time",
                "Handler failure time",
                // Non-deterministic trace/activity ids added when NServiceBus log
                // output is routed through Microsoft.Extensions.Logging to Serilog.
                "SpanId",
                "ParentId");
    }
}
