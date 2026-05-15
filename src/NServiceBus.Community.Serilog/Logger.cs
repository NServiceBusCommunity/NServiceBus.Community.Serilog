// NSB's ILog.*Format methods are contractually string.Format-style ("{0}", "{1:N3}").
// The templates are forwarded to Serilog as-is. Serilog parses "{0}" / "{1}" as valid
// (numeric-named) property tokens, so LogEvent.Properties ends up with keys named "0",
// "1", ... — semantically meaningless but harmless: rendered output is still correct,
// and the template string stays stable for Serilog's template-parser cache. Cleaning
// up the property names would require pre-rendering each call (per-call cache entry)
// or rewriting templates to named placeholders (impossible — NSB owns the call sites).
// This is a deliberate trade in favour of cache stability.
#pragma warning disable CA2254
class Logger(ILogger logger) :
    ILog
{
    public void Debug(string? message) =>
        logger.Debug(message ?? string.Empty);

    public void Debug(string? message, Exception? exception) =>
        logger.Debug(exception, message ?? string.Empty);

    public void DebugFormat(string format, params object?[] args) =>
        logger.Debug(format, args);

    public void Info(string? message) =>
        logger.Information(message ?? string.Empty);

    public void Info(string? message, Exception? exception) =>
        logger.Information(exception, message ?? string.Empty);

    public void InfoFormat(string format, params object?[] args) =>
        logger.Information(format, args);

    public void Warn(string? message) =>
        logger.Warning(message ?? string.Empty);

    public void Warn(string? message, Exception? exception) =>
        logger.Warning(exception, message ?? string.Empty);

    public void WarnFormat(string format, params object?[] args) =>
        logger.Warning(format, args);

    public void Error(string? message) =>
        logger.Error(message ?? string.Empty);

    public void Error(string? message, Exception? exception) =>
        logger.Error(exception, message ?? string.Empty);

    public void ErrorFormat(string format, params object?[] args) =>
        logger.Error(format, args);

    public void Fatal(string? message) =>
        logger.Fatal(message ?? string.Empty);

    public void Fatal(string? message, Exception? exception) =>
        logger.Fatal(exception, message ?? string.Empty);

    public void FatalFormat(string format, params object?[] args) =>
        logger.Fatal(format, args);

    public bool IsDebugEnabled => logger.IsEnabled(LogEventLevel.Debug);
    public bool IsInfoEnabled => logger.IsEnabled(LogEventLevel.Information);
    public bool IsWarnEnabled => logger.IsEnabled(LogEventLevel.Warning);
    public bool IsErrorEnabled => logger.IsEnabled(LogEventLevel.Error);
    public bool IsFatalEnabled => logger.IsEnabled(LogEventLevel.Fatal);
}