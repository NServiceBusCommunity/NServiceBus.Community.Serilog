class DelegatingSink(Action<LogEvent> handler) :
    ILogEventSink
{
    public void Emit(LogEvent logEvent) => handler(logEvent);
}
