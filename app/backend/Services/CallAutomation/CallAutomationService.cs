using System.Collections.Concurrent;

namespace Api.Services.CallAutomation;

public class CallAutomationService : ICallAutomationService
{
    private readonly CallAutomationClient client;
    private readonly IConfiguration configuration;
    private readonly IIdentityService identityService;
    private readonly ICacheService cacheService;
    private readonly ILogger logger;

    private readonly string acsEndpoint;
    private readonly string cgsEndpoint;
    private readonly string cgsLocale;
    private readonly string baseUrl;
    private readonly string wsBaseUrl;

    public CallAutomationService(
        ICacheService cacheService,
        IConfiguration configuration,
        IIdentityService identityService,
        ILogger<CallAutomationService> logger)
    {
        this.cacheService = cacheService;
        this.configuration = configuration;
        this.identityService = identityService;
        this.logger = logger;

        var acsConnectionString = configuration["AcsConnectionString"];
        this.acsEndpoint = configuration["AcsEndpoint"] ?? "";
        this.cgsEndpoint = configuration["CognitiveServiceEndpoint"] ?? "";
        this.cgsLocale = configuration["CognitiveServiceLocale"] ?? "";
        this.baseUrl = configuration["HostUrl"] ?? "";
        ArgumentException.ThrowIfNullOrEmpty(acsConnectionString);
        ArgumentException.ThrowIfNullOrEmpty(acsEndpoint);
        ArgumentException.ThrowIfNullOrEmpty(cgsEndpoint);
        ArgumentException.ThrowIfNullOrEmpty(baseUrl);

        wsBaseUrl = baseUrl.Replace("https://", "wss://", StringComparison.InvariantCultureIgnoreCase);

        this.client = new CallAutomationClient(acsConnectionString);
    }

    public async Task AnswerCallAsync(AcsIncomingCallEventData eventData)
    {
        try
        {
            logger.LogInformation("Received incoming call event");
            var callerId = eventData.FromCommunicationIdentifier.RawId;
            var targetParticipantId = eventData.ToCommunicationIdentifier.RawId;

            var wsHaircutAppointmentVoiceUri = new Uri(
                baseUri: new Uri(this.baseUrl.Replace("https://", "wss://", StringComparison.InvariantCultureIgnoreCase)),
                "/agents/haircut-appointment"
            );

            var wsCustomerAppointmentVoiceUri = new Uri(
                baseUri: new Uri(this.baseUrl.Replace("https://", "wss://", StringComparison.InvariantCultureIgnoreCase)),
                "/agents/customer"
            );

            var wsTranscriptUri = new Uri(
                baseUri: new Uri(this.baseUrl.Replace("https://", "wss://", StringComparison.InvariantCultureIgnoreCase)),
                "/agents/transcript"
            );

            var callbackUrl = new Uri(
                baseUri: new Uri(this.baseUrl),
                relativeUri: $"/api/callbacks/{eventData.ServerCallId}?callerId={callerId}"
            );
            logger.LogInformation("Answering incoming call with callback URL: {0}", callbackUrl);
            
            var callOptions = new AnswerCallOptions(eventData.IncomingCallContext, callbackUrl)
            {
                CallIntelligenceOptions = new CallIntelligenceOptions()
                {
                    CognitiveServicesEndpoint = new Uri(cgsEndpoint)
                },

                // TranscriptionOptions = new TranscriptionOptions(
                //     wsTranscriptUri,
                //     cgsLocale,
                //     false,
                //     TranscriptionTransport.Websocket
                // ),
                MediaStreamingOptions = new MediaStreamingOptions(
                    targetParticipantId.EndsWith("89") ? wsHaircutAppointmentVoiceUri : wsCustomerAppointmentVoiceUri,
                    MediaStreamingContent.Audio,
                    MediaStreamingAudioChannel.Mixed,
                    startMediaStreaming: false
                )
                {
                    EnableBidirectional = true,
                    AudioFormat = AudioFormat.Pcm24KMono
                }
            };

            var result = await client.AnswerCallAsync(callOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not answer incoming call");
            throw;
        }
    }

    public async Task<CreateCallResult> CreateCallAsync(string callerId, string targetParticipant, string threadId = "")
    {
        try
        {
            var callbackUri = new Uri(
                baseUri: new Uri(baseUrl),
                relativeUri: "/api/callbacks" + $"?targetParticipant={targetParticipant}");
            var target = new PhoneNumberIdentifier(targetParticipant);
            var caller = new PhoneNumberIdentifier(callerId);
            var callInvite = new CallInvite(target, caller);
            var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
            {
                OperationContext = threadId
            };

            return await client.CreateCallAsync(createCallOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not create outbound call");
            throw;
        }
    }

    public async Task HandleEvent(CallConnected callConnected)
    {
        var callMedia = GetCallConnection(callConnected.CallConnectionId).GetCallMedia();
        logger.LogInformation("Starting media streaming for call {0}", callConnected.CallConnectionId);
        await callMedia.StartMediaStreamingAsync();
        logger.LogInformation("Starting trasnscription for call {0}", callConnected.CallConnectionId);
        //await callMedia.StartTranscriptionAsync();

    }

    public async Task HandleEvent(CallDisconnected callDisconnected)
    {
        var callMedia = GetCallConnection(callDisconnected.CallConnectionId).GetCallMedia();
        logger.LogInformation("Stopping media streaming for call {0}", callDisconnected.CallConnectionId);
        var output = await callMedia.StopMediaStreamingAsync();
        logger.LogInformation("Stopping transcription for call {0}", callDisconnected.CallConnectionId);
        //await callMedia.StopTranscriptionAsync();
    }

    public CallConnection GetCallConnection(string callConnectionId) =>
        client.GetCallConnection(callConnectionId);
}