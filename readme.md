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

<!-- snippet: SerilogInCode -->
<a id='snippet-SerilogInCode'></a>
```cs
var configuration = new LoggerConfiguration();
configuration.Enrich.WithNsbExceptionDetails();
configuration.WriteTo.File("log.txt");
Log.Logger = configuration.CreateLogger();

LogManager.Use<SerilogFactory>();
```
<sup><a href='/src/Tests/Snippets/Usage.cs#L5-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-SerilogInCode' title='Start of snippet'>anchor</a></sup>
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

LogManager.Use<SerilogFactory>();
```
<sup><a href='/src/Tests/Snippets/Filtering.cs#L5-L21' title='Snippet source file'>snippet source</a> | <a href='#snippet-SerilogFiltering' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Tracing

Writing diagnostic log entries to [Serilog](https://serilog.net/). Plugs into the low level [pipeline](https://docs.particular.net/nservicebus/pipeline) to give more detailed diagnostics.

When using Serilog for tracing, it is optional to use Serilog as the main NServiceBus logger. i.e. there is no need to include `LogManager.Use<SerilogFactory>();`.


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
  logsForTarget: [
    {
      MessageTemplate: Hello from {@Saga}. Message: {@Message},
      Level: Information,
      Properties: {
        ConversationId: Guid_1,
        CorrelationId: Guid_2,
        Handler: TheSaga,
        IncomingMessageId: Guid_2,
        IncomingMessageType: StartSaga,
        IncomingMessageTypeLong: StartSaga, Tests, Version=0.0.0.0,
        Message: {
          TypeTag: StartSaga,
          Properties: [
            {
              Property: TheProperty
            }
          ]
        },
        ProcessingEndpoint: SerilogTestsStartSaga,
        Saga: TheSaga,
        SourceContext: StartSaga
      }
    },
    {
      MessageTemplate: Receive message {IncomingMessageType} {IncomingMessageId} ({ElapsedTime:N3}s).,
      Level: Information,
      Properties: {
        ContentType: text/xml,
        ConversationId: Guid_1,
        CorrelationId: Guid_2,
        ElapsedTime: {Scrubbed},
        FinishTime: DateTimeOffset_1,
        IncomingMessage: {
          TypeTag: StartSaga,
          Properties: [
            {
              Property: TheProperty
            }
          ]
        },
        IncomingMessageId: Guid_2,
        IncomingMessageType: StartSaga,
        IncomingMessageTypeLong: StartSaga, Tests, Version=0.0.0.0,
        MessageIntent: Send,
        OriginatingEndpoint: SerilogTestsStartSaga,
        OriginatingHostId: Guid_3,
        OriginatingMachine: TheMachineName,
        ProcessingEndpoint: SerilogTestsStartSaga,
        ReplyToAddress: SerilogTestsStartSaga,
        Serilog.SagaStateChange: {Scrubbed},
        SourceContext: StartSaga,
        StartTime: DateTimeOffset_2,
        TimeSent: DateTimeOffset_3
      }
    },
    {
      MessageTemplate: Saga execution {SagaType} {SagaId} ({ElapsedTime:N3}s).,
      Level: Information,
      Properties: {
        ConversationId: Guid_1,
        CorrelationId: Guid_2,
        ElapsedTime: {Scrubbed},
        Entity: {
          TypeTag: TheSagaData,
          Properties: [
            {
              Property: TheProperty
            },
            {
              Id: Guid_4
            },
            {
              Originator: SerilogTestsStartSaga
            },
            {
              OriginalMessageId: Guid_2
            }
          ]
        },
        FinishTime: DateTimeOffset_4,
        IncomingMessageId: Guid_2,
        IncomingMessageType: StartSaga,
        IncomingMessageTypeLong: StartSaga, Tests, Version=0.0.0.0,
        Initiator: {
          Elements: {
            "IsSagaTimeout": false,
            "MessageId": Guid_2,
            "OriginatingMachine": TheMachineName,
            "OriginatingEndpoint": SerilogTestsStartSaga,
            "MessageType": StartSaga,
            "TimeSent": DateTimeOffset_5,
            "Intent": Send
          }
        },
        IsCompleted: false,
        IsNew: true,
        ProcessingEndpoint: SerilogTestsStartSaga,
        ResultingMessages: {
          Elements: [
            {
              Elements: {
                "Id": Guid_5,
                "Type": BackIntoSaga,
                "Intent": Send,
                "Destination": SerilogTestsStartSaga
              }
            }
          ]
        },
        SagaId: Guid_4,
        SagaType: TheSaga,
        SourceContext: StartSaga,
        StartTime: DateTimeOffset_6
      }
    },
    {
      MessageTemplate: Sent message {OutgoingMessageType} {OutgoingMessageId}.,
      Level: Information,
      Properties: {
        ContentType: text/xml,
        ConversationId: Guid_1,
        CorrelationId: Guid_2,
        MessageIntent: Send,
        OriginatingEndpoint: SerilogTestsStartSaga,
        OriginatingHostId: Guid_3,
        OriginatingMachine: TheMachineName,
        OutgoingMessage: {
          TypeTag: StartSaga,
          Properties: [
            {
              Property: TheProperty
            }
          ]
        },
        OutgoingMessageId: Guid_2,
        OutgoingMessageType: StartSaga,
        ProcessingEndpoint: SerilogTestsStartSaga,
        ReplyToAddress: SerilogTestsStartSaga,
        SourceContext: StartSaga,
        UnicastRoutes: {
          Elements: [
            SerilogTestsStartSaga
          ]
        }
      }
    },
    {
      MessageTemplate: Sent message {OutgoingMessageType} {OutgoingMessageId}.,
      Level: Information,
      Properties: {
        ContentType: text/xml,
        ConversationId: Guid_1,
        CorrelationId: Guid_2,
        IncomingMessageId: Guid_2,
        IncomingMessageType: StartSaga,
        IncomingMessageTypeLong: StartSaga, Tests, Version=0.0.0.0,
        MessageIntent: Send,
        OriginatingEndpoint: SerilogTestsStartSaga,
        OriginatingHostId: Guid_3,
        OriginatingMachine: TheMachineName,
        OriginatingSagaId: Guid_4,
        OriginatingSagaType: TheSaga,
        OutgoingMessage: {
          TypeTag: BackIntoSaga,
          Properties: [
            {
              Property: TheProperty
            }
          ]
        },
        OutgoingMessageId: Guid_5,
        OutgoingMessageType: BackIntoSaga,
        ProcessingEndpoint: SerilogTestsStartSaga,
        RelatedTo: Guid_2,
        ReplyToAddress: SerilogTestsStartSaga,
        SourceContext: StartSaga,
        UnicastRoutes: {
          Elements: [
            SerilogTestsStartSaga
          ]
        }
      }
    }
  ]
}
```
<sup><a href='/src/Tests/IntegrationTests.Saga.verified.txt#L1-L187' title='Snippet source file'>snippet source</a> | <a href='#snippet-IntegrationTests.Saga.verified.txt' title='Start of snippet'>anchor</a></sup>
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
  logsForTarget: [
    {
      MessageTemplate: Hello from {@Handler}.,
      Level: Information,
      Properties: {
        ConversationId: Guid_1,
        CorrelationId: Guid_2,
        Handler: TheHandler,
        IncomingMessageId: Guid_2,
        IncomingMessageType: StartHandler,
        IncomingMessageTypeLong: StartHandler, Tests, Version=0.0.0.0,
        ProcessingEndpoint: SerilogTestsStartHandler,
        SourceContext: StartHandler
      }
    },
    {
      MessageTemplate: Receive message {IncomingMessageType} {IncomingMessageId} ({ElapsedTime:N3}s).,
      Level: Information,
      Properties: {
        ContentType: application/json,
        ConversationId: Guid_1,
        CorrelationId: Guid_2,
        ElapsedTime: {Scrubbed},
        FinishTime: DateTimeOffset_1,
        IncomingMessage: {
          TypeTag: StartHandler,
          Properties: [
            {
              Property: TheProperty
            }
          ]
        },
        IncomingMessageId: Guid_2,
        IncomingMessageType: StartHandler,
        IncomingMessageTypeLong: StartHandler, Tests, Version=0.0.0.0,
        MessageIntent: Send,
        OpenTelemetry.StartNewTrace: False,
        OriginatingEndpoint: SerilogTestsStartHandler,
        OriginatingHostId: Guid_3,
        OriginatingMachine: TheMachineName,
        ProcessingEndpoint: SerilogTestsStartHandler,
        ReplyToAddress: SerilogTestsStartHandler,
        SourceContext: StartHandler,
        StartTime: DateTimeOffset_2,
        TimeSent: DateTimeOffset_3
      }
    },
    {
      MessageTemplate: Sent message {OutgoingMessageType} {OutgoingMessageId}.,
      Level: Information,
      Properties: {
        ContentType: application/json,
        ConversationId: Guid_1,
        CorrelationId: Guid_2,
        MessageIntent: Send,
        OpenTelemetry.StartNewTrace: False,
        OriginatingEndpoint: SerilogTestsStartHandler,
        OriginatingHostId: Guid_3,
        OriginatingMachine: TheMachineName,
        OutgoingMessage: {
          TypeTag: StartHandler,
          Properties: [
            {
              Property: TheProperty
            }
          ]
        },
        OutgoingMessageId: Guid_2,
        OutgoingMessageType: StartHandler,
        ProcessingEndpoint: SerilogTestsStartHandler,
        ReplyToAddress: SerilogTestsStartHandler,
        Route: SerilogTestsStartHandler,
        SourceContext: StartHandler
      }
    }
  ]
}
```
<sup><a href='/src/Tests/IntegrationTests.Handler.verified.txt#L1-L78' title='Snippet source file'>snippet source</a> | <a href='#snippet-IntegrationTests.Handler.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Startup diagnostics

[Startup diagnostics](https://docs.particular.net/nservicebus/hosting/startup-diagnostics) is, in addition to its default file location, also written to Serilog with the level of `Warning`.

<!-- snippet: WriteStartupDiagnostics -->
<a id='snippet-WriteStartupDiagnostics'></a>
```cs
class StartupDiagnostics(IReadOnlySettings settings, ILogger logger) :
    FeatureStartupTask
{
    protected override Task OnStart(IMessageSession session, Cancel cancel = default)
    {
        var properties = BuildProperties(settings, logger);

        var templateParser = new MessageTemplateParser();
        var messageTemplate = templateParser.Parse("DiagnosticEntries");
        var logEvent = new LogEvent(
            timestamp: DateTimeOffset.Now,
            level: LogEventLevel.Warning,
            exception: null,
            messageTemplate: messageTemplate,
            properties: properties);
        logger.Write(logEvent);
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

    ILogger logger = logger.ForContext<StartupDiagnostics>();
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
<sup><a href='/src/Sample/Program.cs#L3-L10' title='Snippet source file'>snippet source</a> | <a href='#snippet-ConfigureSerilog' title='Start of snippet'>anchor</a></sup>
<a id='snippet-ConfigureSerilog-1'></a>
```cs
var configuration = new LoggerConfiguration();
configuration.Enrich.WithNsbExceptionDetails();
configuration.WriteTo.Seq("http://localhost:5341");
configuration.MinimumLevel.Information();
var logger = configuration.CreateLogger();
var serilogFactory = LogManager.Use<SerilogFactory>();
serilogFactory.WithLogger(logger);
```
<sup><a href='/src/SeqSample/Program.cs#L3-L13' title='Snippet source file'>snippet source</a> | <a href='#snippet-ConfigureSerilog-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Pass the configuration to NServiceBus

<!-- snippet: UseConfig -->
<a id='snippet-UseConfig'></a>
```cs
LogManager.Use<SerilogFactory>();

var configuration = new EndpointConfiguration("SerilogSample");
```
<sup><a href='/src/Sample/Program.cs#L16-L22' title='Snippet source file'>snippet source</a> | <a href='#snippet-UseConfig' title='Start of snippet'>anchor</a></sup>
<a id='snippet-UseConfig-1'></a>
```cs
var configuration = new EndpointConfiguration("SeqSample");
var serilogTracing = configuration.EnableSerilogTracing(tracingLog);
serilogTracing.EnableSagaTracing();
serilogTracing.EnableMessageTracing();
```
<sup><a href='/src/SeqSample/Program.cs#L21-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-UseConfig-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Ensure logging is flushed on shutdown

<!-- snippet: Cleanup -->
<a id='snippet-Cleanup'></a>
```cs
await endpoint.Stop();
Log.CloseAndFlush();
```
<sup><a href='/src/Sample/Program.cs#L37-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-Cleanup' title='Start of snippet'>anchor</a></sup>
<a id='snippet-Cleanup-1'></a>
```cs
await endpoint.Stop();
Log.CloseAndFlush();
```
<sup><a href='/src/SeqSample/Program.cs#L49-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-Cleanup-1' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Sample/Program.cs#L3-L10' title='Snippet source file'>snippet source</a> | <a href='#snippet-ConfigureSerilog' title='Start of snippet'>anchor</a></sup>
<a id='snippet-ConfigureSerilog-1'></a>
```cs
var configuration = new LoggerConfiguration();
configuration.Enrich.WithNsbExceptionDetails();
configuration.WriteTo.Seq("http://localhost:5341");
configuration.MinimumLevel.Information();
var logger = configuration.CreateLogger();
var serilogFactory = LogManager.Use<SerilogFactory>();
serilogFactory.WithLogger(logger);
```
<sup><a href='/src/SeqSample/Program.cs#L3-L13' title='Snippet source file'>snippet source</a> | <a href='#snippet-ConfigureSerilog-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Pass that configuration to NServiceBus

<!-- snippet: UseConfig -->
<a id='snippet-UseConfig'></a>
```cs
LogManager.Use<SerilogFactory>();

var configuration = new EndpointConfiguration("SerilogSample");
```
<sup><a href='/src/Sample/Program.cs#L16-L22' title='Snippet source file'>snippet source</a> | <a href='#snippet-UseConfig' title='Start of snippet'>anchor</a></sup>
<a id='snippet-UseConfig-1'></a>
```cs
var configuration = new EndpointConfiguration("SeqSample");
var serilogTracing = configuration.EnableSerilogTracing(tracingLog);
serilogTracing.EnableSagaTracing();
serilogTracing.EnableMessageTracing();
```
<sup><a href='/src/SeqSample/Program.cs#L21-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-UseConfig-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Ensure logging is flushed on shutdown

<!-- snippet: Cleanup -->
<a id='snippet-Cleanup'></a>
```cs
await endpoint.Stop();
Log.CloseAndFlush();
```
<sup><a href='/src/Sample/Program.cs#L37-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-Cleanup' title='Start of snippet'>anchor</a></sup>
<a id='snippet-Cleanup-1'></a>
```cs
await endpoint.Stop();
Log.CloseAndFlush();
```
<sup><a href='/src/SeqSample/Program.cs#L49-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-Cleanup-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Icon

[Brain](https://thenounproject.com/noun/brain/#icon-No10411) designed by [Rémy Médard](https://thenounproject.com/catalarem) from [The Noun Project](https://thenounproject.com).
