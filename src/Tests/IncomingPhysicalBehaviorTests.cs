public class IncomingPhysicalBehaviorTests
{
    [Test]
    public async Task Empty()
    {
        var logBuilder = new LogBuilder(new FakeLogger(), "endpoint");
        var behavior = new IncomingPhysicalBehavior(logBuilder, "endpoint");
        var context = new RecordingIncomingPhysicalMessageContext();
        Recording.Start();
        await behavior.Invoke(context, TestExtensions.WriteLog);
        await Verify(context);
    }

    [Test]
    public async Task WithMessageTypeFullName()
    {
        var logBuilder = new LogBuilder(new FakeLogger(), "endpoint");
        var behavior = new IncomingPhysicalBehavior(logBuilder, "endpoint");
        var context = new RecordingIncomingPhysicalMessageContext(
            headers: [new(Headers.EnclosedMessageTypes, typeof(Message1).FullName!)]);
        Recording.Start();
        await behavior.Invoke(context, TestExtensions.WriteLog);
        await Verify(context);
    }

    [Test]
    public async Task WithMessageTypeAssemblyQualifiedName()
    {
        var logBuilder = new LogBuilder(new FakeLogger(), "endpoint");
        var behavior = new IncomingPhysicalBehavior(logBuilder, "endpoint");
        var context = new RecordingIncomingPhysicalMessageContext(
            headers: [new(Headers.EnclosedMessageTypes, typeof(Message1).AssemblyQualifiedName!)]);
        Recording.Start();
        await behavior.Invoke(context, TestExtensions.WriteLog);
        await Verify(context);
    }

    [Test]
    public async Task WithMultipleMessageTypesFullName()
    {
        var logBuilder = new LogBuilder(new FakeLogger(), "endpoint");
        var behavior = new IncomingPhysicalBehavior(logBuilder, "endpoint");
        var context = new RecordingIncomingPhysicalMessageContext(
            headers: [new(Headers.EnclosedMessageTypes, $"{typeof(Message1).FullName};{typeof(Message2).FullName}")]);
        Recording.Start();
        await behavior.Invoke(context, TestExtensions.WriteLog);
        await Verify(context);
    }

    [Test]
    public async Task WithMultipleMessageTypesAssemblyQualifiedName()
    {
        var logBuilder = new LogBuilder(new FakeLogger(), "endpoint");
        var behavior = new IncomingPhysicalBehavior(logBuilder, "endpoint");
        var context = new RecordingIncomingPhysicalMessageContext(
            headers: [new(Headers.EnclosedMessageTypes, $"{typeof(Message1).AssemblyQualifiedName};{typeof(Message2).AssemblyQualifiedName}")]);
        Recording.Start();
        await behavior.Invoke(context, TestExtensions.WriteLog);
        await Verify(context);
    }

    [Test]
    public async Task AttachesExceptionLogStateOnThrow()
    {
        var logBuilder = new LogBuilder(new FakeLogger(), "endpoint");
        var behavior = new IncomingPhysicalBehavior(logBuilder, "endpoint");
        var context = new RecordingIncomingPhysicalMessageContext();

        var caught = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Invoke(context, () => throw new InvalidOperationException("boom")));

        await Assert.That(caught!.Message).IsEqualTo("boom");
        await Assert.That(caught.Data.Contains("ExceptionLogState")).IsTrue();
    }

    [Test]
    public async Task ReadOnlyExceptionDataDoesNotHijackOriginalThrow()
    {
        var logBuilder = new LogBuilder(new FakeLogger(), "endpoint");
        var behavior = new IncomingPhysicalBehavior(logBuilder, "endpoint");
        var context = new RecordingIncomingPhysicalMessageContext();

        var caught = await Assert.ThrowsAsync<ReadOnlyDataException>(() =>
            behavior.Invoke(context, () => throw new ReadOnlyDataException("original")));

        await Assert.That(caught!.Message).IsEqualTo("original");
        await Assert.That(caught.Data.Contains("ExceptionLogState")).IsFalse();
    }

    class ReadOnlyDataException(string message) : Exception(message)
    {
        public override IDictionary Data { get; } = new ReadOnlyDictionary();

        class ReadOnlyDictionary : IDictionary
        {
            public bool IsReadOnly => true;
            public bool IsFixedSize => true;
            public ICollection Keys => Array.Empty<object>();
            public ICollection Values => Array.Empty<object>();
            public int Count => 0;
            public bool IsSynchronized => false;
            public object SyncRoot { get; } = new();

            public object? this[object key]
            {
                get => null;
                set => throw new NotSupportedException();
            }

            public bool Contains(object key) => false;
            public void Add(object key, object? value) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public IDictionaryEnumerator GetEnumerator() => new Hashtable().GetEnumerator();
            public void Remove(object key) => throw new NotSupportedException();
            public void CopyTo(Array array, int index) { }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    class Message1;

    class Message2;
}
