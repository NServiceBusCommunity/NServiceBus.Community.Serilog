#pragma warning disable 1591

class ExceptionLogState(LogEventProperty processingEndpoint, IReadOnlyDictionary<string, string> incomingHeaders, string? correlationId, string? conversationId)
{
    public readonly LogEventProperty ProcessingEndpoint = processingEndpoint;
    public readonly string? CorrelationId = correlationId;
    public readonly string? ConversationId = conversationId;
    public object? IncomingMessage;
    public readonly IReadOnlyDictionary<string, string> IncomingHeaders = incomingHeaders;
}