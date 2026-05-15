[NotInParallel("HeaderAppender.frozen")]
public class HeaderAppenderTests
{
    [Test]
    public async Task ExcludeAddsHeadersBeforeFreeze()
    {
        HeaderAppender.ResetForTests();
        try
        {
            #region ExcludeHeaders
            HeaderAppender.Exclude("MyCustomHeader");
            HeaderAppender.Exclude("HeaderA", "HeaderB", "HeaderC");
            #endregion

            var headers = new Dictionary<string, string>
            {
                ["MyCustomHeader"] = "0",
                ["HeaderA"] = "1",
                ["HeaderB"] = "2",
                ["HeaderC"] = "3",
                ["HeaderD"] = "4"
            };
            var properties = HeaderAppender.BuildHeaders(headers, convertHeader: null).ToList();

            await Assert.That(properties).HasSingleItem();
            var other = (DictionaryValue) properties[0].Value;
            await Assert.That(other.Elements.Keys.Select(_ => (string) _.Value!)).IsEquivalentTo(["HeaderD"]);
        }
        finally
        {
            HeaderAppender.ResetForTests();
            HeaderAppender.Freeze();
        }
    }

    [Test]
    public void ExcludeParamsThrowsAfterFreeze()
    {
        HeaderAppender.ResetForTests();
        HeaderAppender.Freeze();

        Assert.Throws<InvalidOperationException>(() => HeaderAppender.Exclude("A", "B"));
    }

    [Test]
    public async Task ExcludeThrowsAfterFreeze()
    {
        HeaderAppender.ResetForTests();
        HeaderAppender.Freeze();

        var exception = Assert.Throws<InvalidOperationException>(() => HeaderAppender.Exclude("TooLate"));

        await Assert.That(exception.Message).Contains("frozen");
    }
}
