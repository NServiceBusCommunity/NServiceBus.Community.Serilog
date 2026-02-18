using TypeNameConverter = NServiceBus.Serilog.TypeNameConverter;
public class TypeNameConverterTests
{
    [Test]
    public void NameOnly() =>
        AreEqual("TheClass", TypeNameConverter.GetName("TheClass"));

    [Test]
    public void NameAndNamespace() =>
        AreEqual("TheClass", TypeNameConverter.GetName("Namespace.TheClass"));

    [Test]
    public void NameAndNamespaceAndAssembly() =>
        AreEqual("TheClass", TypeNameConverter.GetName("Namespace.TheClass, Tests"));

    [Test]
    public void AssemblyQualified()
    {
        AreEqual("TheClass", TypeNameConverter.GetName("Namespace.TheClass, Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce8ec7717ba6fbb6"));
        AreEqual("TheClass", TypeNameConverter.GetName("Namespace.TheClass, Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=ce8ec7717ba6fbb6"));
        AreEqual("TheClass", TypeNameConverter.GetName("Namespace.TheClass, Foo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=ce8ec7717ba6fbb6"));
    }

    [Test]
    public void AssemblyQualifiedWithNoVersion() =>
        AreEqual("TheClass", TypeNameConverter.GetName("Namespace.TheClass, Tests"));
}

namespace Namespace
{
    // ReSharper disable once UnusedType.Global
    class TheClass;
}