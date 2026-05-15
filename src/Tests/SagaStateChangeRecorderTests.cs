public class SagaStateChangeRecorderTests
{
    [Test]
    public async Task EmptyByDefault()
    {
        var recorder = new SagaStateChangeRecorder();
        await Assert.That(recorder.Value).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task SingleEntry()
    {
        var recorder = new SagaStateChangeRecorder();
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        recorder.Record(id, "New");
        await Assert.That(recorder.Value).IsEqualTo($"{id}:New");
    }

    [Test]
    public async Task MultipleEntriesJoinedBySemicolon()
    {
        var recorder = new SagaStateChangeRecorder();
        var id1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var id2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        recorder.Record(id1, "New");
        recorder.Record(id2, "Updated");
        await Assert.That(recorder.Value).IsEqualTo($"{id1}:New;{id2}:Updated");
    }
}
