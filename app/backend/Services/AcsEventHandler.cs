// using Api.Models;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Newtonsoft.Json;

namespace Api.Services;

public interface IAcsEventHandler
{
    Task<IResult> IncomingCallAsync(EventGridEvent[] eventGridEvents);
    IResult Callback(CloudEvent[] cloudEvents, string contextId, string callerId);
}

public class AcsEventHandler : IAcsEventHandler
{
    public readonly CallAutomationClient _client;
    public readonly ILogger<AcsEventHandler> _logger;
    public readonly IConfiguration _configuration;

    public AcsEventHandler(IConfiguration configuration, ILogger<AcsEventHandler> logger)
    {
        this._configuration = configuration;

        var acsConnectionString = configuration.GetValue<string>("AcsConnectionString");
        ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

        var acsEndpoint = configuration.GetValue<string>("AcsEndpoint");
        ArgumentNullException.ThrowIfNullOrEmpty(acsEndpoint);

        this._logger = logger;
        this._client = new CallAutomationClient(
            // new Uri(acsEndpoint),
            acsConnectionString
        );
    }

    /// <summary>
    /// Handle callback event
    /// </summary>
    /// <param name="cloudEvents"></param>
    /// <param name="contextId"></param>
    /// <param name="callerId"></param>
    /// <returns></returns>    
    public IResult Callback(CloudEvent[] cloudEvents, string contextId, string callerId)
    {
        var eventProcessor = this._client.GetEventProcessor();
        eventProcessor.ProcessEvents(cloudEvents);

        foreach (var cloudEvent in cloudEvents)
        {
            CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
            _logger.LogInformation(
                        "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId} {event}",
                        parsedEvent.GetType(),
                        parsedEvent.CallConnectionId,
                        parsedEvent.ServerCallId,
                        parsedEvent.ToString());
        }
        return Results.Ok();
    }

    /// <summary>
    /// Handle incoming call event
    /// </summary>
    /// <param name="eventGridEvents"></param>
    /// <returns></returns>
    public async Task<IResult> IncomingCallAsync(EventGridEvent[] eventGridEvents)
    {
        foreach (var eventGridEvent in eventGridEvents)
        {
            _logger.LogInformation($"Call event received:{eventGridEvent.EventType}");

            // Handle system events
            if (eventGridEvent.TryGetSystemEventData(out object eventData))
            {
                if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
                {
                    var responseData = new SubscriptionValidationResponse
                    {
                        ValidationResponse = subscriptionValidationEventData.ValidationCode
                    };

                    return Results.Ok(responseData);
                }
            }

            if (eventData is AcsIncomingCallEventData incomingCallEventData)
            {
                var callerId = incomingCallEventData.FromCommunicationIdentifier.RawId;

                var callbackUriHost = _configuration.GetValue<string>("CallbackUriHost");
                ArgumentNullException.ThrowIfNullOrEmpty(callbackUriHost);

                var callbackUri = new Uri(new Uri(callbackUriHost), $"/api/events/callbacks/{Guid.NewGuid()}?callerId={callerId}");
                var websocketTranscriptUri = new Uri(new Uri(callbackUriHost.Replace("https://", "wss://", StringComparison.InvariantCultureIgnoreCase)), "/ws/transcript");
                var websocketVoiceUri = new Uri(new Uri(callbackUriHost.Replace("https://", "wss://", StringComparison.InvariantCultureIgnoreCase)), "/ws/voice");

                _logger.LogInformation($"Callback Url: {callbackUri}");
                _logger.LogInformation($"Transcript WebSocket Url: {websocketTranscriptUri}");
                _logger.LogInformation($"Voice WebSocket Url: {websocketVoiceUri}");

                // Transcription options
                TranscriptionOptions? transcriptionOptions = null;
                CallIntelligenceOptions? callIntelligenceOptions = null;
                MediaStreamingOptions? mediaStreamingOptions = null;

                var cognitiveServiceLocale = _configuration.GetValue<string>("CognitiveServiceLocale");
                ArgumentNullException.ThrowIfNullOrEmpty(cognitiveServiceLocale);

                var cognitiveServiceEndpoint = _configuration.GetValue<string>("CognitiveServiceEndpoint");
                ArgumentNullException.ThrowIfNullOrEmpty(cognitiveServiceEndpoint);

                transcriptionOptions = new TranscriptionOptions(
                    websocketTranscriptUri,
                    cognitiveServiceLocale,
                    false,
                    TranscriptionTransport.Websocket
                );

                callIntelligenceOptions = new CallIntelligenceOptions()
                {
                    CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint)
                };

                mediaStreamingOptions = new MediaStreamingOptions(
                    websocketVoiceUri,
                    MediaStreamingContent.Audio,
                    MediaStreamingAudioChannel.Mixed,
                    startMediaStreaming: false
                )
                {
                    EnableBidirectional = true,
                    AudioFormat = AudioFormat.Pcm24KMono
                };

                // Answer the call
                var options = new AnswerCallOptions(incomingCallEventData.IncomingCallContext, callbackUri)
                {
                    CallIntelligenceOptions = callIntelligenceOptions ?? null,
                    TranscriptionOptions = transcriptionOptions ?? null,
                    MediaStreamingOptions = mediaStreamingOptions ?? null
                };

                _logger.LogInformation($"Answering call with options: {JsonConvert.SerializeObject(options)}");
                AnswerCallResult answerCallResult = await this._client.AnswerCallAsync(options);
                var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();

                var answerResult = await answerCallResult.WaitForEventProcessorAsync();
                if (answerResult.IsSuccess)
                {
                    // Call currentCall = this.callStore.CreateCall(answerResult.SuccessResult.CallConnectionId, callerId);

                    /* Start the recording */
                    // CallLocator callLocator = new ServerCallLocator(answerResult.SuccessResult.ServerCallId);
                    // var recordingResult = await this.client.GetCallRecording().StartAsync(new StartRecordingOptions(callLocator));
                    // currentCall.RecordingId = recordingResult.Value.RecordingId;
                    // this.callStore.UpdateCall(currentCall);
                    // logger.LogInformation($"Recording started. RecordingId: {currentCall.RecordingId}");

                    // Start transcription
                    await callConnectionMedia.StartMediaStreamingAsync();
                    await callConnectionMedia.StartTranscriptionAsync();
                    _logger.LogInformation("Transcription initiated.");
                }

                _client.GetEventProcessor().AttachOngoingEventProcessor<CallDisconnected>(
                answerCallResult.CallConnection.CallConnectionId, async (callDisconnectedEvent) =>
                {
                    _logger.LogInformation($"Received call event: {callDisconnectedEvent.GetType()}");
                });

                _client.GetEventProcessor().AttachOngoingEventProcessor<TranscriptionStarted>(
                answerCallResult.CallConnection.CallConnectionId, async (transcriptionStarted) =>
                {
                    _logger.LogInformation($"Received transcription event: {transcriptionStarted.GetType()}");
                });

                _client.GetEventProcessor().AttachOngoingEventProcessor<TranscriptionStopped>(
                    answerCallResult.CallConnection.CallConnectionId, async (transcriptionStopped) =>
                    {
                        _logger.LogInformation("Received transcription event: {type}", transcriptionStopped.GetType());
                    });

                _client.GetEventProcessor().AttachOngoingEventProcessor<TranscriptionFailed>(
                    answerCallResult.CallConnection.CallConnectionId, async (TranscriptionFailed) =>
                    {
                        _logger.LogInformation($"Received transcription event: {TranscriptionFailed.GetType()}, CorrelationId: {TranscriptionFailed.CorrelationId}, " +
                            $"SubCode: {TranscriptionFailed?.ResultInformation?.SubCode}, Message: {TranscriptionFailed?.ResultInformation?.Message}");
                    });
            }
        }

        return Results.Ok();
    }
}