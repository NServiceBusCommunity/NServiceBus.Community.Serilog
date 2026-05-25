# <img src="/src/icon.png" height="30px"> NServiceBus.Community.Serilog

[![Build status](https://img.shields.io/appveyor/build/SimonCropp/nservicebus-community-serilog)](https://ci.appveyor.com/project/SimonCropp/nservicebus-community-serilog)
[![NuGet Status](https://img.shields.io/nuget/v/NServiceBus.Community.Serilog.svg)](https://www.nuget.org/packages/NServiceBus.Community.Serilog/)

Add support for sending [NServiceBus](http://particular.net/NServiceBus) logging through [Serilog](http://serilog.net/)

**See [Milestones](../../milestones?state=closed) for release notes.**

<!--- StartOpenCollectiveBackers -->

[Already a Patron? skip past this section](#endofbacking)


## Community backed

**It is expected that all developers [become a Patron](https://opencollective.com/nservicebuscommunity/contribute/patron-6976) to use NServiceBus Community Extensions. [Go to licensing FAQ](https://github.com/NServiceBusCommunity/Home/#licensingpatron-faq)**


### Sponsors

Support this project by [becoming a Sponsor](https://opencollective.com/nservicebuscommunity/contribute/sponsor-6972). The company avatar will show up here with a website link. The avatar will also be added to all GitHub repositories under the [NServiceBusCommunity organization](https://github.com/NServiceBusCommunity).


### Patrons

Thanks to all the backing developers. Support this project by [becoming a patron](https://opencollective.com/nservicebuscommunity/contribute/patron-6976).

<img src="https://opencollective.com/nservicebuscommunity/tiers/patron.svg?width=890&avatarHeight=60&button=false">

<a href="#" id="endofbacking"></a>

<!--- EndOpenCollectiveBackers -->


## NuGet package

https://nuget.org/packages/NServiceBus.Community.Serilog/


## Usage

NServiceBus reads its logging configuration from [Microsoft.Extensions.Logging](https://learn.microsoft.com/dotnet/core/extensions/logging). To send NServiceBus log output to Serilog, configure a Serilog logger and route it into the host's logging builder via `AddSerilog()` (from the [Serilog.Extensions.Logging](https://www.nuget.org/packages/Serilog.Extensions.Logging) package).

<!-- snippet: SerilogInCode -->
<a id='snippet-SerilogInCode'></a>
```cs
var configuration = new LoggerConfiguration();
configuration.Enrich.WithNsbExceptionDetails();
configuration.WriteTo.File("log.txt");
Log.Logger = configuration.CreateLogger();

var builder = Host.CreateApplicationBuilder();
builder.Logging.AddSerilog();
```
<sup><a href='/src/Tests/Snippets/Usage.cs#L5-L15' title='Snippet source file'>snippet source</a> | <a href='#snippet-SerilogInCode' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Filtering

NServiceBus can write a significant amount of information to the log. To limit this information use the filtering features of the underlying logging framework.

For example to limit log output to a specific namespace.

Here is a code configuration example for adding a [Filter](https://github.com/serilog/serilog/wiki/Configuration-Basics#filters).

<!-- snippet: SerilogFiltering -->
<a id='snippet-SerilogFiltering'></a>
```cs
var configuration = new LoggerConfiguration();
configuration.Enrich.WithNsbExceptionDetails();
configuration
    .WriteTo.File(
        path: "log.txt",
        restrictedToMinimumLevel: LogEventLevel.Debug
    );
configuration
    .Filter.ByIncludingOnly(
        inclusionPredicate: Matching.FromSource("MyNamespace"));
Log.Logger = configuration.CreateLogger();

var builder = Host.CreateApplicationBuilder();
builder.Logging.AddSerilog();
```
<sup><a href='/src/Tests/Snippets/Filtering.cs#L5-L22' title='Snippet source file'>snippet source</a> | <a href='#snippet-SerilogFiltering' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Tracing

Writing diagnostic log entries to [Serilog](https://serilog.net/). Plugs into the low level [pipeline](https://docs.particular.net/nservicebus/pipeline) to give more detailed diagnostics.

When using Serilog for tracing, it is optional to use Serilog as the main NServiceBus logger. i.e. there is no need to call `builder.Logging.AddSerilog()`.


### Create an instance of a Serilog logger

<!-- snippet: SerilogTracingLogger -->
<a id='snippet-SerilogTracingLogger'></a>
```cs
var configuration = new LoggerConfiguration();
configuration.Enrich.WithNsbExceptionDetails();
configuration.WriteTo.File("log.txt");
configuration.MinimumLevel.Information();
var tracingLog = configuration.CreateLogger();
```
<sup><a href='/src/Tests/Snippets/TracingUsage.cs#L21-L29' title='Snippet source file'>snippet source</a> | <a href='#snippet-SerilogTracingLogger' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Configure the tracing feature to use that logger

<!-- snippet: SerilogTracingPassLoggerToFeature -->
<a id='snippet-SerilogTracingPassLoggerToFeature'></a>
```cs
var serilogTracing = configuration.EnableSerilogTracing(tracingLog);
serilogTracing.EnableMessageTracing();
```
<sup><a href='/src/Tests/Snippets/TracingUsage.cs#L11-L16' title='Snippet source file'>snippet source</a> | <a href='#snippet-SerilogTracingPassLoggerToFeature' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Contextual logger

Serilog tracing injects a contextual `Serilog.Ilogger` into the NServiceBus pipeline.

NOTE: Saga and message tracing will use the current contextual logger.

There are several layers of enrichment based on the pipeline phase.


#### Endpoint enrichment

All loggers for an endpoint will have the property `ProcessingEndpoint` added that contains the current [endpoint name](https://docs.particular.net/nservicebus/endpoints/specify-endpoint-name).


#### Incoming message enrichment

When a message is received, the following enrichment properties are added:

 * [SourceContext](https://github.com/serilog/serilog/wiki/Writing-Log-Events#source-contexts) will be the message type [FullName](https://docs.microsoft.com/de-de/dotnet/api/system.type.fullname) extracted from the [EnclosedMessageTypes header](https://docs.particular.net/nservicebus/messaging/headers#serialization-headers-nservicebus-enclosedmessagetypes). `UnknownMessageType` will be used if no header exists. The same value will be added to a property named `MessageType`.
 * `MessageId` will be the value of the [MessageId header](https://docs.particular.net/nservicebus/messaging/headers#messaging-interaction-headers-nservicebus-messageid).
 * `CorrelationId` will be the value of the [CorrelationId header](https://docs.particular.net/nservicebus/messaging/headers#messaging-interaction-headers-nservicebus-correlationid) if it exists.
 * `ConversationId` will be the value of the [ConversationId header](https://docs.particular.net/nservicebus/messaging/headers#messaging-interaction-headers-nservicebus-conversationid) if it exists.


#### Handler enrichment

When a handler is invoked, a new logger is forked from the above enriched physical logger with a new enriched property named `Handler` that contains the [FullName](https://docs.microsoft.com/de-de/dotnet/api/system.type.fullname) of the current handler.


#### Outgoing message enrichment

When a message is sent, the same properties as described in "Incoming message enrichment" will be added to the outgoing pipeline. Note that if a handler sends a message, the logger injected into the outgoing pipeline will be forked from the logger instance as described in "Handler enrichment". As such it will contain a property `Handler` for the handler that sent the message.


#### Ambient LogContext enrichment

While the incoming message is being processed, `IncomingMessageId`, `CorrelationId`, and `ConversationId` are also pushed onto Serilog's ambient [`LogContext`](https://github.com/serilog/serilog/wiki/Enrichment#the-logcontext). This lets log events written via the static `Log.Logger` — including events emitted from code that has no access to `context.Logger()`, such as third-party library callbacks — carry the same correlation properties as this library's own tracing events.

To opt in, the consumer's logger must be configured with `Enrich.FromLogContext()`. Without it, the values are still pushed but no enricher consumes them, so the properties will not appear on log events.

```cs
var configuration = new LoggerConfiguration();
configuration.Enrich.FromLogContext();
configuration.Enrich.WithNsbExceptionDetails();
configuration.WriteTo.File("log.txt");
Log.Logger = configuration.CreateLogger();
```


#### Accessing the logger

The contextual logger instance can be accessed from anywhere in the pipeline via `SerilogTracingExtensions.Logger(this IPipelineContext context)`.

<!-- snippet: ContextualLoggerUsage -->
<a id='snippet-ContextualLoggerUsage'></a>
```cs
public class HandlerUsingLogger :
    IHandleMessages<TheMessage>
{
    public Task Handle(TheMessage message, HandlerContext context)
    {
        var logger = context.Logger();
        logger.Information("Hello from {@Handler}.");
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Tests/Snippets/ContextualLoggerUsage.cs#L1-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-ContextualLoggerUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Log extension methods

`IPipelineContext` also has extension methods added to expose direct `Log*` methods

<!-- snippet: DirectLogUsage -->
<a id='snippet-DirectLogUsage'></a>
```cs
public class HandlerUsingLog :
    IHandleMessages<TheMessage>
{
    public Task Handle(TheMessage message, HandlerContext context)
    {
        context.LogInformation("Hello from {@Handler}.");
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Tests/Snippets/ContextualLoggerUsage.cs#L16-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-DirectLogUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Example result of a contextual log entry

<img src="/src/contextualLog.png">


### Exception enrichment

When an exception occurs in the message processing pipeline, the current pipeline state is added to the exception. When that exception is logged that state can be add to the log entry.

When a pipeline exception is logged, it will be enriched with the following properties:

 * `ProcessingEndpoint` will be the current [endpoint name](https://docs.particular.net/nservicebus/endpoints/specify-endpoint-name).
 * `IncomingMessageId` will be the value of the [MessageId header](https://docs.particular.net/nservicebus/messaging/headers#messaging-interaction-headers-nservicebus-messageid).
 * `IncomingTransportMessageId` will be the MessageId from the underlying [transport](https://docs.particular.net/transports/) if it exist.
 * `IncomingHeaders` will be the value of the [Message headers](https://docs.particular.net/nservicebus/messaging/headers).
 * `IncomingMessageType` will be the message type [FullName](https://docs.microsoft.com/de-de/dotnet/api/system.type.fullname) extracted from the [EnclosedMessageTypes header](https://docs.particular.net/nservicebus/messaging/headers#serialization-headers-nservicebus-enclosedmessagetypes). `UnknownMessageType` will be used if no header exists.
 * `CorrelationId` will be the value of the [CorrelationId header](https://docs.particular.net/nservicebus/messaging/headers#messaging-interaction-headers-nservicebus-correlationid) if it exists.
 * `ConversationId` will be the value of the [ConversationId header](https://docs.particular.net/nservicebus/messaging/headers#messaging-interaction-headers-nservicebus-conversationid) if it exists.
 * `HandlerType` will be type name for the current handler if it exists.
 * `IncomingMessage` will be the value of current logical message if it exists.
 * `HandlerStartTime` the UTC timestamp for when the handler started.
 * `HandlerFailureTime` the UTC timestamp for when the handler threw the exception.


### Excluding headers

`HeaderAppender.BuildHeaders` (used by message audit, saga audit, and exception enrichment) promotes message headers to log event properties. By default a small set of NServiceBus infrastructure headers (`EnclosedMessageTypes`, `ProcessingEndpoint`, `CorrelationId`, `ConversationId`, `NServiceBusVersion`, `MessageId`) is excluded. Additional header names can be added to that exclude set via `HeaderAppender.Exclude`:

<!-- snippet: ExcludeHeaders -->
<a id='snippet-ExcludeHeaders'></a>
```cs
HeaderAppender.Exclude("MyCustomHeader");
HeaderAppender.Exclude("HeaderA", "HeaderB", "HeaderC");
```
<sup><a href='/src/Tests/HeaderAppenderTests.cs#L10-L13' title='Snippet source file'>snippet source</a> | <a href='#snippet-ExcludeHeaders' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`Exclude` must be called during application startup, **before** the endpoint is started. Once the endpoint has started the exclude set is frozen and any subsequent call to `Exclude` throws `InvalidOperationException`. This makes the set effectively immutable for the lifetime of the endpoint and eliminates any race between configuration and the running pipeline.


### Saga tracing

<!-- snippet: EnableSagaTracing -->
<a id='snippet-EnableSagaTracing'></a>
```cs
var serilogTracing = configuration.EnableSerilogTracing(logger);
serilogTracing.EnableSagaTracing();
```
<sup><a href='/src/Tests/Snippets/TracingUsage.cs#L36-L41' title='Snippet source file'>snippet source</a> | <a href='#snippet-EnableSagaTracing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Example Logs

<!-- snippet: IntegrationTests.Saga.verified.txt -->
<a id='snippet-IntegrationTests.Saga.verified.txt'></a>
```txt
{
  log: [
    {
      Debug: {State:l},
      Properties: {
        Endpoint: SerilogTestsStartSaga,
        ParentId: {Scrubbed},
        SourceContext: NServiceBus.SerializeMessageConnector,
        SpanId: {Scrubbed},
        State: Serializing message 'StartSaga, Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce8ec7717ba6fbb6' with id 'Guid_1', ToString() of the message yields: StartSaga,
        TraceId: Guid_2
      }
    },
    {
      Information: Sent message {OutgoingMessageType} {OutgoingMessageId}.,
      Properties: {
        ContentType: application/json,
        ConversationId: Guid_3,
        CorrelationId: Guid_1,
        MessageIntent: Send,
        OpenTelemetry.StartNewTrace: False,
        OriginatingEndpoint: SerilogTestsStartSaga,
        OriginatingHostId: Guid_4,
        OriginatingMachine: TheMachineName,
        OutgoingMessage: {
          TypeTag: StartSaga,
          Property: TheProperty
        },
        OutgoingMessageId: Guid_1,
        OutgoingMessageType: StartSaga,
        ProcessingEndpoint: SerilogTestsStartSaga,
        ReplyToAddress: SerilogTestsStartSaga,
        Route: SerilogTestsStartSaga,
        SourceContext: StartSaga
      }
    },
    {
      Information: Hello from {@Saga}. Message: {@Message},
      Properties: {
        ConversationId: Guid_3,
        CorrelationId: Guid_1,
        Handler: TheSaga,
        IncomingMessageId: Guid_1,
        IncomingMessageType: StartSaga,
        IncomingMessageTypeLong: StartSaga, Tests, Version=0.0.0.0,
        Message: {
          TypeTag: StartSaga,
          Property: TheProperty
        },
        ProcessingEndpoint: SerilogTestsStartSaga,
        Saga: TheSaga,
        SourceContext: StartSaga
      }
    },
    {
      Debug: {State:l},
      Properties: {
        ConversationId: Guid_3,
        CorrelationId: Guid_1,
        Endpoint: SerilogTestsStartSaga,
        IncomingMessageId: Guid_1,
        ParentId: {Scrubbed},
        SourceContext: NServiceBus.SerializeMessageConnector,
        SpanId: {Scrubbed},
        State: Serializing message 'BackIntoSaga, Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce8ec7717ba6fbb6' with id 'Guid_5', ToString() of the message yields: BackIntoSaga,
        TraceId: Guid_2
      }
    },
    {
      Information: Sent message {OutgoingMessageType} {OutgoingMessageId}.,
      Properties: {
        ContentType: application/json,
        ConversationId: Guid_3,
        CorrelationId: Guid_1,
        IncomingMessageId: Guid_1,
        IncomingMessageType: StartSaga,
        IncomingMessageTypeLong: StartSaga, Tests, Version=0.0.0.0,
        MessageIntent: Send,
        OpenTelemetry.StartNewTrace: False,
        OriginatingEndpoint: SerilogTestsStartSaga,
        OriginatingHostId: Guid_4,
        OriginatingMachine: TheMachineName,
        OriginatingSagaId: Guid_6,
        OriginatingSagaType: TheSaga,
        OutgoingMessage: {
          TypeTag: BackIntoSaga,
          Property: TheProperty
        },
        OutgoingMessageId: Guid_5,
        OutgoingMessageType: BackIntoSaga,
        ProcessingEndpoint: SerilogTestsStartSaga,
        RelatedTo: Guid_1,
        ReplyToAddress: SerilogTestsStartSaga,
        Route: SerilogTestsStartSaga,
        SourceContext: StartSaga
      }
    },
    {
      Information: Saga execution {SagaType} {SagaId} ({ElapsedTime:N3}s).,
      Properties: {
        ConversationId: Guid_3,
        CorrelationId: Guid_1,
        ElapsedTime: {Scrubbed},
        Entity: {
          TypeTag: TheSagaData,
          Property: TheProperty,
          Id: Guid_6,
          Originator: SerilogTestsStartSaga,
          OriginalMessageId: Guid_1
        },
        FinishTime: {Scrubbed},
        IncomingMessageId: Guid_1,
        IncomingMessageType: StartSaga,
        IncomingMessageTypeLong: StartSaga, Tests, Version=0.0.0.0,
        Initiator: {
          IsSagaTimeout: false,
          MessageId: Guid_1,
          OriginatingMachine: TheMachineName,
          OriginatingEndpoint: SerilogTestsStartSaga,
          MessageType: StartSaga,
          TimeSent: DateTimeOffset_1,
          Intent: Send
        },
        IsCompleted: false,
        IsNew: true,
        ProcessingEndpoint: SerilogTestsStartSaga,
        ResultingMessages: {
          Elements: [
            {
              Id: Guid_5,
              Type: BackIntoSaga,
              Intent: Send,
              Destination: SerilogTestsStartSaga
            }
          ]
        },
        SagaId: Guid_6,
        SagaType: TheSaga,
        SourceContext: StartSaga,
        StartTime: {Scrubbed}
      }
    },
    {
      Information: Receive message {IncomingMessageType} {IncomingMessageId} ({ElapsedTime:N3}s).,
      Properties: {
        ContentType: application/json,
        ConversationId: Guid_3,
        CorrelationId: Guid_1,
        ElapsedTime: {Scrubbed},
        FinishTime: {Scrubbed},
        IncomingMessage: {
          TypeTag: StartSaga,
          Property: TheProperty
        },
        IncomingMessageId: Guid_1,
        IncomingMessageType: StartSaga,
        IncomingMessageTypeLong: StartSaga, Tests, Version=0.0.0.0,
        MessageIntent: Send,
        OpenTelemetry.StartNewTrace: False,
        OriginatingEndpoint: SerilogTestsStartSaga,
        OriginatingHostId: Guid_4,
        OriginatingMachine: TheMachineName,
        OtherHeaders: {
          baggage: {Scrubbed}
        },
        ProcessingEndpoint: SerilogTestsStartSaga,
        ReplyToAddress: SerilogTestsStartSaga,
        Serilog.SagaStateChange: {Scrubbed},
        SourceContext: StartSaga,
        StartTime: {Scrubbed},
        TimeSent: DateTimeOffset_1,
        TraceParent: {Scrubbed}
      }
    },
    {
      Debug: {State:l},
      Properties: {
        Endpoint: SerilogTestsStartSaga,
        ParentId: {Scrubbed},
        SourceContext: NServiceBus.Pipeline`1[[NServiceBus.Pipeline.IBatchDispatchContext, NServiceBus.Core, Version=10.0.0.0, Culture=neutral, PublicKeyToken=9fc386479f8a226c]],
        SpanId: {Scrubbed},
        State:
(IBatchDispatchContext context0) => BatchToDispatchConnector.Invoke(context0,
    (IDispatchContext context1) => ImmediateDispatchTerminator.Invoke(context1)),
        TraceId: Guid_2
      }
    },
    {
      Information: Hello from {@Saga}. Message: {@Message},
      Properties: {
        ConversationId: Guid_3,
        CorrelationId: Guid_1,
        Handler: TheSaga,
        IncomingMessageId: Guid_5,
        IncomingMessageType: BackIntoSaga,
        IncomingMessageTypeLong: BackIntoSaga, Tests, Version=0.0.0.0,
        Message: {
          TypeTag: BackIntoSaga,
          Property: TheProperty
        },
        ProcessingEndpoint: SerilogTestsStartSaga,
        Saga: TheSaga,
        SourceContext: BackIntoSaga
      }
    }
  ]
}
```
<sup><a href='/src/Tests/IntegrationTests.Saga.verified.txt#L1-L207' title='Snippet source file'>snippet source</a> | <a href='#snippet-IntegrationTests.Saga.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Message tracing

Both incoming and outgoing messages will be logged at the [Information level](https://github.com/serilog/serilog/wiki/Writing-Log-Events#the-role-of-the-information-level). The current message will be included in a property named `Message`. For outgoing messages any unicast routes will be included in a property named `UnicastRoutes`.

<!-- snippet: EnableMessageTracing -->
<a id='snippet-EnableMessageTracing'></a>
```cs
var serilogTracing = configuration.EnableSerilogTracing(logger);
serilogTracing.EnableMessageTracing();
```
<sup><a href='/src/Tests/Snippets/TracingUsage.cs#L46-L51' title='Snippet source file'>snippet source</a> | <a href='#snippet-EnableMessageTracing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Example Logs

<!-- snippet: IntegrationTests.Handler.verified.txt -->
<a id='snippet-IntegrationTests.Handler.verified.txt'></a>
```txt
{
  log: [
    {
      Debug: {State:l},
      Properties: {
        Endpoint: SerilogTestsStartHandler,
        ParentId: {Scrubbed},
        SourceContext: NServiceBus.SerializeMessageConnector,
        SpanId: {Scrubbed},
        State: Serializing message 'StartHandler, Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce8ec7717ba6fbb6' with id 'Guid_1', ToString() of the message yields: StartHandler,
        TraceId: Guid_2
      }
    },
    {
      Information: Sent message {OutgoingMessageType} {OutgoingMessageId}.,
      Properties: {
        ContentType: application/json,
        ConversationId: Guid_3,
        CorrelationId: Guid_1,
        MessageIntent: Send,
        OpenTelemetry.StartNewTrace: False,
        OriginatingEndpoint: SerilogTestsStartHandler,
        OriginatingHostId: Guid_4,
        OriginatingMachine: TheMachineName,
        OutgoingMessage: {
          TypeTag: StartHandler,
          Property: TheProperty
        },
        OutgoingMessageId: Guid_1,
        OutgoingMessageType: StartHandler,
        ProcessingEndpoint: SerilogTestsStartHandler,
        ReplyToAddress: SerilogTestsStartHandler,
        Route: SerilogTestsStartHandler,
        SourceContext: StartHandler
      }
    },
    {
      Information: Hello from {@Handler}.,
      Properties: {
        ConversationId: Guid_3,
        CorrelationId: Guid_1,
        Handler: TheHandler,
        IncomingMessageId: Guid_1,
        IncomingMessageType: StartHandler,
        IncomingMessageTypeLong: StartHandler, Tests, Version=0.0.0.0,
        ProcessingEndpoint: SerilogTestsStartHandler,
        SourceContext: StartHandler
      }
    }
  ]
}
```
<sup><a href='/src/Tests/IntegrationTests.Handler.verified.txt#L1-L51' title='Snippet source file'>snippet source</a> | <a href='#snippet-IntegrationTests.Handler.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Startup diagnostics

[Startup diagnostics](https://docs.particular.net/nservicebus/hosting/startup-diagnostics) is, in addition to its default file location, also written to Serilog with the level of `Warning`.

<!-- snippet: WriteStartupDiagnostics -->
<a id='snippet-WriteStartupDiagnostics'></a>
```cs
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
```
<sup><a href='/src/NServiceBus.Community.Serilog/StartupDiagnostics/WriteStartupDiagnostics.cs#L1-L58' title='Snippet source file'>snippet source</a> | <a href='#snippet-WriteStartupDiagnostics' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Logging to Seq

To log to [Seq](https://getseq.net/):

<!-- snippet: SerilogTracingSeq -->
<a id='snippet-SerilogTracingSeq'></a>
```cs
var configuration = new LoggerConfiguration();
configuration.Enrich.WithNsbExceptionDetails();
configuration.WriteTo.Seq("http://localhost:5341");
configuration.MinimumLevel.Information();
var tracingLog = configuration.CreateLogger();
```
<sup><a href='/src/Tests/Snippets/TracingUsage.cs#L56-L64' title='Snippet source file'>snippet source</a> | <a href='#snippet-SerilogTracingSeq' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Sample

The sample illustrates how to customize logging by configuring Serilog targets and rules.


### Configure Serilog

<!-- snippet: ConfigureSerilog -->
<a id='snippet-ConfigureSerilog'></a>
```cs
var configuration = new LoggerConfiguration();
configuration.Enrich.WithNsbExceptionDetails();
configuration.WriteTo.Console();
Log.Logger = configuration.CreateLogger();
```
<sup><a href='/src/Sample/Program.cs#L7-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-ConfigureSerilog' title='Start of snippet'>anchor</a></sup>
<a id='snippet-ConfigureSerilog-1'></a>
```cs
var configuration = new LoggerConfiguration();
configuration.Enrich.WithNsbExceptionDetails();
configuration.WriteTo.Seq("http://localhost:5341");
configuration.MinimumLevel.Information();
var logger = configuration.CreateLogger();
```
<sup><a href='/src/SeqSample/Program.cs#L7-L15' title='Snippet source file'>snippet source</a> | <a href='#snippet-ConfigureSerilog-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Pass the configuration to NServiceBus

<!-- snippet: UseConfig -->
<a id='snippet-UseConfig'></a>
```cs
var builder = Host.CreateApplicationBuilder();
builder.Logging.AddSerilog();
builder.Services.AddNServiceBusEndpoint(configuration);
```
<sup><a href='/src/Sample/Program.cs#L29-L35' title='Snippet source file'>snippet source</a> | <a href='#snippet-UseConfig' title='Start of snippet'>anchor</a></sup>
<a id='snippet-UseConfig-1'></a>
```cs
var configuration = new EndpointConfiguration("SeqSample");
var serilogTracing = configuration.EnableSerilogTracing(tracingLog);
serilogTracing.EnableSagaTracing();
serilogTracing.EnableMessageTracing();
```
<sup><a href='/src/SeqSample/Program.cs#L23-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-UseConfig-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Ensure logging is flushed on shutdown

<!-- snippet: Cleanup -->
<a id='snippet-Cleanup'></a>
```cs
await host.StopAsync();
Log.CloseAndFlush();
```
<sup><a href='/src/Sample/Program.cs#L45-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-Cleanup' title='Start of snippet'>anchor</a></sup>
<a id='snippet-Cleanup-1'></a>
```cs
await host.StopAsync();
Log.CloseAndFlush();
```
<sup><a href='/src/SeqSample/Program.cs#L57-L62' title='Snippet source file'>snippet source</a> | <a href='#snippet-Cleanup-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Seq Sample

Illustrates customizing [Serilog](https://serilog.net/) usage to log to [Seq](https://getseq.net/).


### Prerequisites

An instance of [Seq](https://getseq.net/) running one `http://localhost:5341`.


### Configure Serilog

<!-- snippet: ConfigureSerilog -->
<a id='snippet-ConfigureSerilog'></a>
```cs
var configuration = new LoggerConfiguration();
configuration.Enrich.WithNsbExceptionDetails();
configuration.WriteTo.Console();
Log.Logger = configuration.CreateLogger();
```
<sup><a href='/src/Sample/Program.cs#L7-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-ConfigureSerilog' title='Start of snippet'>anchor</a></sup>
<a id='snippet-ConfigureSerilog-1'></a>
```cs
var configuration = new LoggerConfiguration();
configuration.Enrich.WithNsbExceptionDetails();
configuration.WriteTo.Seq("http://localhost:5341");
configuration.MinimumLevel.Information();
var logger = configuration.CreateLogger();
```
<sup><a href='/src/SeqSample/Program.cs#L7-L15' title='Snippet source file'>snippet source</a> | <a href='#snippet-ConfigureSerilog-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Pass that configuration to NServiceBus

<!-- snippet: UseConfig -->
<a id='snippet-UseConfig'></a>
```cs
var builder = Host.CreateApplicationBuilder();
builder.Logging.AddSerilog();
builder.Services.AddNServiceBusEndpoint(configuration);
```
<sup><a href='/src/Sample/Program.cs#L29-L35' title='Snippet source file'>snippet source</a> | <a href='#snippet-UseConfig' title='Start of snippet'>anchor</a></sup>
<a id='snippet-UseConfig-1'></a>
```cs
var configuration = new EndpointConfiguration("SeqSample");
var serilogTracing = configuration.EnableSerilogTracing(tracingLog);
serilogTracing.EnableSagaTracing();
serilogTracing.EnableMessageTracing();
```
<sup><a href='/src/SeqSample/Program.cs#L23-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-UseConfig-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Ensure logging is flushed on shutdown

<!-- snippet: Cleanup -->
<a id='snippet-Cleanup'></a>
```cs
await host.StopAsync();
Log.CloseAndFlush();
```
<sup><a href='/src/Sample/Program.cs#L45-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-Cleanup' title='Start of snippet'>anchor</a></sup>
<a id='snippet-Cleanup-1'></a>
```cs
await host.StopAsync();
Log.CloseAndFlush();
```
<sup><a href='/src/SeqSample/Program.cs#L57-L62' title='Snippet source file'>snippet source</a> | <a href='#snippet-Cleanup-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Icon

[Brain](https://thenounproject.com/noun/brain/#icon-No10411) designed by [Rémy Médard](https://thenounproject.com/catalarem) from [The Noun Project](https://thenounproject.com).
