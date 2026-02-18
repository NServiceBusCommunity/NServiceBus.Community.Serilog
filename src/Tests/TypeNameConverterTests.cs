using TypeNameConverter = NServiceBus.Serilog.TypeNameConverter;
public class TypeNameConverterTests
{
    [Test]
    public async Task NameOnly() =>
        await Assert.That(TypeNameConverter.GetName("TheClass")).IsEqualTo("TheClass");

    [Test]
    public async Task NameAndNamespace() =>
        await Assert.That(TypeNameConverter.GetName("Namespace.TheClass")).IsEqualTo("TheClass");

    [Test]
    public async Task NameAndNamespaceAndAssembly() =>
        await Assert.That(TypeNameConverter.GetName("Namespace.TheClass, Tests")).IsEqualTo("TheClass");

    [Test]
    public async Task AssemblyQualified()
    {
        await Assert.That(TypeNameConverter.GetName("Namespace.TheClass, Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce8ec7717ba6fbb6")).IsEqualTo("TheClass");
        await Assert.That(TypeNameConverter.GetName("Namespace.TheClass, Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=ce8ec7717ba6fbb6")).IsEqualTo("TheClass");
        await Assert.That(TypeNameConverter.GetName("Namespace.TheClass, Foo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=ce8ec7717ba6fbb6")).IsEqualTo("TheClass");
    }

    [Test]
    public async Task AssemblyQualifiedWithNoVersion() =>
        await Assert.That(TypeNameConverter.GetName("Namespace.TheClass, Tests")).IsEqualTo("TheClass");
}

namespace Namespace
{
    // ReSharper disable once UnusedType.Global
    class TheClass;
}
