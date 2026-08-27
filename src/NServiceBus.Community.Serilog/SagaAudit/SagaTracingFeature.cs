class SagaTracingFeature :
    Feature
{
    public SagaTracingFeature()
    {
        DependsOn<Sagas>();
        DependsOn<TracingFeature>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings.TracingSettings();
        var pipeline = context.Pipeline;
        pipeline.Register(new CaptureSagaStateBehavior.Registration(settings.captureLimits));
        pipeline.Register(new CaptureSagaResultingBehavior.Registration());
    }
}