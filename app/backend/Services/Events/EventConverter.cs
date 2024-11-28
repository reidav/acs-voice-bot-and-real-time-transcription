namespace Api.Services.Events;

public sealed class EventConverter
{
    internal const string CallEndedEventName = "CallEnded";
    internal const string ChatMessageReceivedInThreadName = "ChatMessageReceivedInThread";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public object? Convert(EventGridEvent eventGridEvent)
    {
        var data = eventGridEvent.Data;
        if (data is null) throw new ArgumentNullException($"No data present: {eventGridEvent}");

        return ParseEventType(eventGridEvent.EventType) switch
        {
            // ChatMessageReceivedInThreadName => data.ToObjectFromJson<AcsChatMessageReceivedInThreadEventData>(JsonOptions),
            // CallEndedEventName => data.ToObjectFromJson<CallEndedEvent>(JsonOptions),
            _ => null
        };
    }

    private static string ParseEventType(string eventType)
    {
        var split = eventType.Split("Microsoft.Communication.");
        return split[^1];
    }
}