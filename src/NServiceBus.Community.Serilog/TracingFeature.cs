class TracingFeature :
    Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        HeaderAppender.Freeze();

        var settings = context.Settings.TracingSettings();

        var endpoint = context.Settings.EndpointName();
        var logBuilder = new LogBuilder(settings.Logger, endpoint);

        var pipeline = context.Pipeline;
        pipeline.Register(new IncomingPhysicalBehavior.Registration(logBuilder, endpoint));
        pipeline.Register(new IncomingLogicalBehavior.Registration(logBuilder));
        pipeline.Register(new HandlerContextBehavior.Registration());
        pipeline.Register(new MessageContextBehavior.Registration());
        pipeline.Register(new OutgoingBehavior.Registration(logBuilder));
        context.RegisterStartupTask(new StartupDiagnostics(context.Settings, logBuilder.Logger));
    }
}