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
                "TraceParent");
        VerifierSettings.AddExtraDateTimeOffsetFormat("yyyy-MM-ddTHH:mm:ss.fffzz");

        LogManager.Use<SerilogFactory>();
    }
}
