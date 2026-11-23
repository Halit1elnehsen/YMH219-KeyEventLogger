namespace YMH219_KeyEventLogger;

public sealed record KeyboardEventRecord(DateTime Timestamp, string KeyName, string EventName)
{
    public string ToDisplayString() => $"{Timestamp:HH:mm:ss.fff}    {EventName,-8}    {KeyName}";
    public string ToFileString() => $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff}\t{EventName}\t{KeyName}";
}
