class MessageTracingFeature :
    Feature
{
    public MessageTracingFeature() =>
        DependsOn<TracingFeature>();

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings.TracingSettings();
        var pipeline = context.Pipeline;
        pipeline.Register(new LogIncomingBehavior.Registration(settings.convertHeader, settings.captureLimits));
        pipeline.Register(new LogOutgoingBehavior.Registration(settings.convertHeader, settings.captureLimits));
    }
}