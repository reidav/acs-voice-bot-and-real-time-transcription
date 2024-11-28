using System.ClientModel;
using System.Threading.Channels;
using Api.Agents.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace Api.Agents;

#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public abstract class VoiceAgent : IVoiceAgent
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Instructions { get; }
    public ConversationVoice Voice { get; }
    public bool IsTranscriptEnabled { get; }
    public abstract IList<ITool> Tools { get; }

    public ILoggerFactory LoggerFactory { get; init; } =  NullLoggerFactory.Instance;
    protected ILogger Logger => this._logger ??= this.LoggerFactory.CreateLogger(this.GetType());
    private ILogger? _logger;

    private readonly WebSocket ws;
    private readonly CancellationTokenSource cts_ws;
    public readonly CancellationTokenSource cts_rt;
    public readonly RealtimeConversationSession rtConversationSession;
    private Channel<Func<Task>> channel;
    protected readonly IConfiguration configuration;
    public VoiceAgent(
        WebSocket ws,
        IConfiguration configuration)
    {
        this.ws = ws;
        this.cts_ws = new CancellationTokenSource();
        this.cts_rt = new CancellationTokenSource();
        this.rtConversationSession = CreateRealTimeConversationSessionAsync(configuration).GetAwaiter().GetResult();
        this.channel = Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

        this.configuration = configuration;
    }

    private async Task<RealtimeConversationSession> CreateRealTimeConversationSessionAsync(IConfiguration configuration)
    {
        var openAiKey = configuration.GetValue<string>("AzureOpenAIServiceKey");
        ArgumentNullException.ThrowIfNullOrEmpty(openAiKey);

        var openAiUri = configuration.GetValue<string>("AzureOpenAIServiceEndpoint");
        ArgumentNullException.ThrowIfNullOrEmpty(openAiUri);

        var openAiModelName = configuration.GetValue<string>("AzureOpenAIDeploymentModelName");
        ArgumentNullException.ThrowIfNullOrEmpty(openAiModelName);

        var aiClient = new AzureOpenAIClient(new Uri(openAiUri), new ApiKeyCredential(openAiKey));
        var RealtimeCovnClient = aiClient.GetRealtimeConversationClient(openAiModelName);
        var session = await RealtimeCovnClient.StartConversationSessionAsync();

        var sessionOption = new ConversationSessionOptions
        {
            Instructions = this.Instructions,
            Voice = ConversationVoice.Alloy,
            InputAudioFormat = ConversationAudioFormat.Pcm16,
            OutputAudioFormat = ConversationAudioFormat.Pcm16,
            InputTranscriptionOptions = IsTranscriptEnabled ? new()
            {
                Model = "whisper-1",
            } : null,
            TurnDetectionOptions = ConversationTurnDetectionOptions.CreateServerVoiceActivityTurnDetectionOptions(0.5f, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500)),
        };
        await session.ConfigureSessionAsync(sessionOption);
        return session;
    }

    public async Task Process()
    {
        if (this.ws == null)
            return;

        // start forwarder to AI model
        _ = Task.Run(async () => await StartForwardingAudioToMediaStreaming());

        try
        {
            _ = Task.Run(async () => await GetOpenAiStreamResponseAsync());
            while (this.ws.State == WebSocketState.Open || this.ws.State == WebSocketState.Closed)
            {
                byte[] receiveBuffer = new byte[2048];
                WebSocketReceiveResult receiveResult = await this.ws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), this.cts_ws.Token);

                if (receiveResult.MessageType != WebSocketMessageType.Close)
                {
                    string data = Encoding.UTF8.GetString(receiveBuffer).TrimEnd('\0');

                    var input = StreamingData.Parse(data);
                    if (input is AudioData audioData)
                    {
                        using (var ms = new MemoryStream(audioData.Data))
                        {
                            await this.rtConversationSession.SendAudioAsync(ms);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Exception -> {ex}");
        }
        finally
        {
            this.Close();
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (this.ws?.State == WebSocketState.Open)
        {
            // Console.WriteLine($"{message}");
            byte[] jsonBytes = Encoding.UTF8.GetBytes(message);

            // Send the PCM audio chunk over WebSocket
            await this.ws.SendAsync(new ArraySegment<byte>(jsonBytes), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
        }
    }

    public async Task GetOpenAiStreamResponseAsync()
    {
        try
        {
            await this.rtConversationSession.StartResponseTurnAsync();

            await foreach (ConversationUpdate update in this.rtConversationSession.ReceiveUpdatesAsync(this.cts_rt.Token))
            {
                if (update is ConversationSessionStartedUpdate sessionStartedUpdate)
                {
                    Logger.LogInformation($"<<< Session started. ID: {sessionStartedUpdate.SessionId}");
                }

                if (update is ConversationInputSpeechStartedUpdate speechStartedUpdate)
                {
                    Logger.LogInformation(
                        $"  -- Voice activity detection started at {speechStartedUpdate.AudioStartMs} ms");
                    // Barge-in, received stop audio
                    StopAudio();
                }

                if (update is ConversationInputSpeechFinishedUpdate speechFinishedUpdate)
                {
                    Logger.LogInformation(
                        $"  -- Voice activity detection ended at {speechFinishedUpdate.AudioEndMs} ms");
                }

                if (update is ConversationItemStartedUpdate itemStartedUpdate)
                {
                    Logger.LogInformation($"  -- Begin streaming of new item");
                }

                if (update is ConversationAudioDeltaUpdate audioDeltaUpdate)
                {
                    ConvertToAcsAudioPacketAndForward(audioDeltaUpdate.Delta.ToArray());
                }

                if (update is ConversationItemFinishedUpdate itemFinishedUpdate)
                {
                    Logger.LogInformation($"  -- Item streaming finished, response_id={itemFinishedUpdate.ResponseId}");

                    if (itemFinishedUpdate.FunctionCallId is not null)
                    {
                        Logger.LogInformation($"    + Function call name: {itemFinishedUpdate.FunctionName}");
                        Logger.LogInformation($"    + Function call ID: {itemFinishedUpdate.FunctionCallId}");
                        Logger.LogInformation($"    + Function call param: {itemFinishedUpdate.FunctionCallArguments}");

                        var tool = this.Tools.Where(t => t.Definition.Name == itemFinishedUpdate.FunctionName).FirstOrDefault();
                        if (tool is not null)
                        {
                            var toolResponse = await tool.ExecuteAsync(itemFinishedUpdate.FunctionCallArguments);
                            Logger.LogInformation($"    + Tool response: {toolResponse}");

                            ConversationItem functionOutputItem =
                              ConversationItem.CreateFunctionCallOutput(
                                callId: itemFinishedUpdate.FunctionCallId,
                                output: toolResponse
                              );

                            await rtConversationSession.AddItemAsync(functionOutputItem);
                        }
                    }
                }

                if (update is ConversationInputTranscriptionFinishedUpdate transcriptionCompletedUpdate)
                {
                    Logger.LogInformation($"  -- User audio transcript: {transcriptionCompletedUpdate.Transcript}");
                }

                if (update is ConversationResponseFinishedUpdate turnFinishedUpdate)
                {
                    Logger.LogInformation($"  -- Model turn generation finished. Status: {turnFinishedUpdate.Status}");
                    // Here, if we processed tool calls in the course of the model turn, we finish the
                    // client turn to resume model generation. The next model turn will reflect the tool
                    // responses that were already provided.
                    if (turnFinishedUpdate.CreatedItems.Any(item => item.FunctionName?.Length > 0))
                    {
                        Logger.LogInformation($"  -- Ending client turn for pending tool responses");
                        await rtConversationSession.StartResponseTurnAsync();
                    }
                }

                if (update is ConversationErrorUpdate errorUpdate)
                {
                    Logger.LogInformation($"ERROR: {errorUpdate.ErrorMessage}");
                    break;
                }
            }
        }
        catch (OperationCanceledException e)
        {
            Logger.LogError($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Exception during ai streaming -> {ex}");
        }
    }


    private async Task StartForwardingAudioToMediaStreaming()
    {
        try
        {
            // Consume messages from channel and forward buffers to player
            while (true)
            {
                var processBuffer = await channel.Reader.ReadAsync(this.cts_rt.Token).ConfigureAwait(false);
                await processBuffer.Invoke();
            }
        }
        catch (OperationCanceledException opCanceledException)
        {
            Console.WriteLine($"OperationCanceledException received for StartForwardingAudioToPlayer : {opCanceledException}");
        }
        catch (ObjectDisposedException objDisposedException)
        {
            Console.WriteLine($"ObjectDisposedException received for StartForwardingAudioToPlayer :{objDisposedException}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception received for StartForwardingAudioToPlayer {ex}");
        }
    }

    public void ConvertToAcsAudioPacketAndForward(byte[] audioData)
    {
        var jsonString = OutStreamingData.GetAudioDataForOutbound(audioData);
        ReceiveAudioForOutBound(jsonString);
    }

    public void StopAudio()
    {
        try
        {
            var jsonString = OutStreamingData.GetStopAudioForOutbound();
            ReceiveAudioForOutBound(jsonString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during streaming -> {ex}");
        }
    }

    private void ReceiveAudioForOutBound(string data)
    {
        try
        {
            this.channel.Writer.TryWrite(async () => await this.SendMessageAsync(data));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\"Exception received on ReceiveAudioForOutBound {ex}");
        }
    }

    public void Close()
    {
        this.cts_ws.Cancel();
        this.cts_ws.Dispose();
        this.cts_rt.Cancel();
        this.cts_rt.Dispose();
        this.rtConversationSession.Dispose();
    }
}