class Usage
{
    Usage()
    {
        #region SerilogInCode

        var configuration = new LoggerConfiguration();
        configuration.Enrich.WithNsbExceptionDetails();
        configuration.WriteTo.File("log.txt");
        Log.Logger = configuration.CreateLogger();

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.AddSerilog();

        #endregion
    }

    static void Seq()
    {
        #region SerilogSeq

        var configuration = new LoggerConfiguration();
        configuration.Enrich.WithNsbExceptionDetails();
        configuration.WriteTo.Seq("http://localhost:5341");
        configuration.MinimumLevel.Information();
        Log.Logger = configuration.CreateLogger();

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.AddSerilog();

        #endregion
    }
}
