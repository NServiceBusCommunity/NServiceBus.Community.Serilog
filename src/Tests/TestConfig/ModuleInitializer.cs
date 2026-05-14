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
            _.Filter.ByExcluding(logEvent => logEvent.MessageTemplate.Text.StartsWith("Operation canceled while stopping"));
        });
        VerifierSettings.InitializePlugins();
        var nsbVersion = FileVersionInfo.GetVersionInfo(typeof(Endpoint).Assembly.Location);
        var nsbVersionString = $"{nsbVersion.FileMajorPart}.{nsbVersion.FileMinorPart}.{nsbVersion.FileBuildPart}";
        VerifierSettings.IgnoreStackTrace();
        VerifierSettings.AddScrubber(_ => _.Replace(nsbVersionString, "NsbVersion"));
        VerifierSettings.ScrubMachineName();
        VerifierSettings.AddExtraDateTimeOffsetFormat("yyyy-MM-dd HH:mm:ss:ffffff Z");
        VerifierSettings
            .ScrubMembers(
                "ElapsedTime",
                "TraceParent",
                "Task",
                "TargetSite");
        VerifierSettings.ScrubLinesContaining(
            "NServiceBus.TimeSent :",
            "NServiceBus.ConversationId :",
            "NServiceBus.Retries.Timestamp :",
            "NServiceBus.DeliverAt :",
            "NServiceBus.TimeOfFailure :",
            "NServiceBus.ExceptionInfo.Data.Handler start time :",
            "NServiceBus.ExceptionInfo.Data.Handler failure time :",
            "NServiceBus.ExceptionInfo.Data.Transport message ID :",
            "traceparent :",
            "tunit.test.id",
            "Completing processing for");
        VerifierSettings.AddExtraDateTimeOffsetFormat("yyyy-MM-ddTHH:mm:ss.fffzz");

        LogManager.Use<SerilogFactory>();
    }
}
