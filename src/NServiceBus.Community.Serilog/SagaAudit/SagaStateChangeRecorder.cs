class SagaStateChangeRecorder
{
    string value = string.Empty;

    public void Record(Guid sagaId, string stateChange)
    {
        if (value.Length > 0)
        {
            value += ';';
        }

        value += $"{sagaId}:{stateChange}";
    }

    public string Value => value;
}
