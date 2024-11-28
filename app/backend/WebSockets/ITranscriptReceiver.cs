namespace Api.WebSockets;

public interface ITranscriptReceiver
{
    event EventHandler<TranscriptReceivedEventArgs>? TranscriptCompleted;

    Task ProcessRequest(WebSocket webSocket);
}