using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using Azure.Communication.CallAutomation;

namespace Api.Services;

public interface ITranscriptService
{
    event EventHandler<TranscriptEventArgs>? TranscriptCompleted;

    Task ProcessRequest(WebSocket webSocket);
}

public class TranscriptEventArgs : EventArgs
{
    private TranscriptionMetadata _metadata;
    private TranscriptionData _data;

    public TranscriptEventArgs(TranscriptionMetadata metadata, TranscriptionData data)
    {
        this._metadata = metadata;
        this._data = data;
    }

    public TranscriptionMetadata Metadata { get { return this._metadata; } }

    public TranscriptionData Data { get { return this._data; } }
}

public class TranscriptService(ILogger<TranscriptService> logger) : ITranscriptService
{
    public event EventHandler<TranscriptEventArgs>? TranscriptCompleted;


    public async Task ProcessRequest(WebSocket webSocket)
    {
        try
        {
            while (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseSent)
            {
                var buffer = new byte[1024 * 4];
                var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;
                WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                TranscriptionMetadata? metadata = null;

                while (!receiveResult.CloseStatus.HasValue)
                {
                    string msg = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                    logger.LogInformation($"Received message: {msg}");

                    var response = StreamingDataParser.Parse(msg);

                    if (response is TranscriptionMetadata transcriptionMetadata)
                    {
                        logger.LogInformation("***************************************************************************************");
                        logger.LogInformation("TRANSCRIPTION SUBSCRIPTION ID-->" + transcriptionMetadata.TranscriptionSubscriptionId);
                        logger.LogInformation("LOCALE-->" + transcriptionMetadata.Locale);
                        logger.LogInformation("CALL CONNECTION ID--?" + transcriptionMetadata.CallConnectionId);
                        logger.LogInformation("CORRELATION ID-->" + transcriptionMetadata.CorrelationId);
                        logger.LogInformation("***************************************************************************************");
                        metadata = transcriptionMetadata;
                    }
                    if (response is TranscriptionData transcriptionData)
                    {
                        logger.LogInformation("***************************************************************************************");
                        logger.LogInformation("TEXT-->" + transcriptionData.Text);
                        logger.LogInformation("FORMAT-->" + transcriptionData.Format);
                        logger.LogInformation("OFFSET-->" + transcriptionData.Offset.Ticks);
                        logger.LogInformation("DURATION-->" + transcriptionData.Duration.Ticks);
                        logger.LogInformation("PARTICIPANT-->" + transcriptionData.Participant.RawId);
                        logger.LogInformation("CONFIDENCE-->" + transcriptionData.Confidence);
                        logger.LogInformation("RESULT STATUS-->" + transcriptionData.ResultState);
                        foreach (var word in transcriptionData.Words)
                        {
                            logger.LogInformation("WORDS TEXT-->" + word.Text);
                            logger.LogInformation("WORDS OFFSET-->" + word.Offset.Ticks);
                            logger.LogInformation("WORDS DURATION-->" + word.Duration.Ticks);
                        }
                        logger.LogInformation("***************************************************************************************");

                        TranscriptCompleted?.Invoke(this, new TranscriptEventArgs(metadata!, transcriptionData));                        
                    }

                    await webSocket.SendAsync(
                        new ArraySegment<byte>(buffer, 0, receiveResult.Count),
                        receiveResult.MessageType,
                        receiveResult.EndOfMessage,
                        CancellationToken.None);

                    receiveResult = await webSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer), CancellationToken.None);
                }

                await webSocket.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            logger.LogInformation($"Exception -> {ex}");
        }
    }
}