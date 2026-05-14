# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and test

All projects live under `src/`. The SDK is pinned by `global.json` (`10.0.300`, `rollForward: latestFeature`, prerelease allowed). Tests use Microsoft.Testing.Platform (`UseMicrosoftTestingPlatformRunner`) via TUnit + Verify, not VSTest.

```pwsh
dotnet build src --configuration Release
dotnet test --solution src/NServiceBus.Serilog.sln --configuration Release --no-build --no-restore
```

Run a single test (TUnit / MTP filter syntax):

```pwsh
dotnet run --project src/Tests --configuration Debug -- --filter-method "*Handler"
```

Build settings to be aware of (`src/Directory.Build.props`):

- `TreatWarningsAsErrors=true` and `EnforceCodeStyleInBuild=true` — warnings and analyzer style violations fail the build.
- `LangVersion=preview`, `ImplicitUsings=enable`. Common namespaces (`NServiceBus.*`, `Serilog.*`, etc.) are injected as `<Using>` in the main csproj — don't add explicit `using` directives for those.
- Package versions are centralized in `src/Directory.Packages.props` (`ManagePackageVersionsCentrally=true`). Add version numbers there, not in csproj files.
- `IMessageHandlerContext` is aliased globally as `HandlerContext`.

The `Saga` integration test is gated behind `#if Debug` in `src/Tests/IntegrationTests.cs` — it only runs in Debug builds.

## Architecture

The package exposes two largely independent integration points with NServiceBus:

### 1. NSB logging → Serilog (`SerilogFactory`)

`LogManager.Use<SerilogFactory>()` plugs Serilog in as the underlying `LoggingFactoryDefinition` for NSB's own log output. `LoggerFactory` wraps a Serilog `ILogger` and produces `Logger` instances that implement NSB's `ILog` by forwarding to `ForContext(type)` / `ForContext("SourceContext", name)`. This path does NOT require enabling any feature — it's a pure logging redirect.

### 2. Pipeline tracing (`EnableSerilogTracing`)

`SerilogTracingExtensions.EnableSerilogTracing(EndpointConfiguration, ILogger)` stashes a `SerilogTracingSettings` on the NSB settings bag and enables `TracingFeature`. `TracingFeature.Setup` builds a `LogBuilder` (root logger enriched with `ProcessingEndpoint`) and registers pipeline behaviors:

- `LogInjection/IncomingPhysicalBehavior` / `IncomingLogicalBehavior` — enrich the contextual logger with message-level properties (`MessageId`, `MessageType`, `CorrelationId`, `ConversationId`, headers) and stash it under context extension key `"SerilogIncomingLogger"`.
- `LogInjection/HandlerContextBehavior` / `MessageContextBehavior` — fork a handler-specific logger (`"SerilogHandlerLogger"`) with a `Handler` property.
- `LogInjection/OutgoingBehavior` — stashes `"SerilogOutgoingLogger"` for outgoing pipeline.
- `MessageAudit/*` (opt-in via `EnableMessageTracing()`) — writes Information-level entries `Receive message {IncomingMessageType}…` and `Sent message {OutgoingMessageType}…`.
- `SagaAudit/*` (opt-in via `EnableSagaTracing()`) — captures saga state before/after invocation and logs `Saga execution …`. Off by default.
- `StartupDiagnostics/WriteStartupDiagnostics` — runs as a `FeatureStartupTask`, writes NSB startup diagnostic entries as a single `Warning` log event.

`SerilogTracingExtensions.Logger(IPipelineContext)` is the canonical accessor: it tries `SerilogOutgoingLogger`, then `SerilogHandlerLogger`, then a plain `ILogger` stashed in extensions. There's a special-case for `TestableMessageHandlerContext` that falls back to `Log.Logger` so handlers using `Logger()` work in `NServiceBus.Testing`.

### Exception enrichment

`ExceptionEnricher` (registered via `Enrich.WithNsbExceptionDetails()`) pulls keys NSB sets on `Exception.Data` (`Message type`, `Message ID`, `Transport message ID`, `Handler type`, `Handler start time`, `Handler failure time`, and the internal `ExceptionLogState` carrying `ProcessingEndpoint`/`IncomingMessage`/headers) and promotes them to log event properties via `AddPropertyIfAbsent`. `ExceptionLogState` is attached to thrown exceptions in `LogInjection/IncomingLogicalBehavior`.

### Header → property conversion

`HeaderAppender.BuildHeaders` is shared by the incoming/outgoing behaviors and `ExceptionEnricher`. It strips `NServiceBus.` prefixes, special-cases a handful of headers (`TimeSent`, `OriginatingSagaType`, `OriginatingHostId`, `HostDisplayName`, `HostId`, `DiagnosticsTraceParent` → `TraceParent`), skips an excluded set (`EnclosedMessageTypes`, `ProcessingEndpoint`, correlation/conversation/message id, NSB version), and bundles whatever's left into an `OtherHeaders` dictionary property. Users can extend the exclude set via `HeaderAppender.Exclude(name)` or override individual conversions per-endpoint via `SerilogTracingSettings.UseHeaderConversion(ConvertHeader)`.

## Tests

`src/Tests` uses Verify snapshots (`.verified.txt`) and the `VerifyTests.NServiceBus` plugin. Custom Verify configuration in `src/Tests/TestConfig/ModuleInitializer.cs`:

- Custom JSON converters for `LogEvent`, `LogEventProperty`, `ScalarValue`, `PropertyEnricher` so snapshots stay readable.
- Scrubs `ElapsedTime` and `TraceParent` members; scrubs machine name; replaces the NSB assembly version with the literal `NsbVersion`.

`IntegrationTests` spins up a real endpoint via `ConfigBuilder.BuildDefaultConfig` and an `EventSink` that captures `LogEvent` instances into a static list. The `Send` helper sets a fixed message id (`00000000-…-0001`) and waits on a `ManualResetEvent` set by a recoverability `OnMessageSentToErrorQueue` callback — failing tests typically time out at the 10-second wait.

## README

`readme.md` is generated by `MarkdownSnippets.MsBuild` (config in `src/mdsnippets.json`, `Convention: InPlaceOverwrite`). Snippet regions are referenced from source files via `<!-- snippet: Name -->`; do not hand-edit the snippet bodies in `readme.md` — change the source `.cs` and the build regenerates the markdown.

## CI

AppVeyor (`src/appveyor.yml`) installs the SDK from `global.json`, builds, then runs tests against the solution. There is also a GitHub Actions docs workflow (`.github/workflows/on-push-do-docs.yml`).
