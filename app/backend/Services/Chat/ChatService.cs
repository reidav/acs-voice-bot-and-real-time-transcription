namespace Api.Services.Chat;

public class ChatService : IChatService
{
    private readonly IIdentityService identityService;
    private readonly ICacheService cacheService;
    private readonly ICallAutomationService callAutomationService;
    private readonly IConfiguration configuration;
    private readonly ILogger logger;
    private readonly string acsEndpoint;
    private readonly string acsOutboundCallerId;
    private readonly string botUserId;
    private const string PSTNRegex = @"(\+\d{1,3}[-.\s]??\d{10}|\d{3}[-.\s]??\d{3}[-.\s]??\d{4}|\(\d{3}\)[-.\s]??\d{3}[-.\s]??\d{4})";

    public ChatService(
        IIdentityService identityService,
        ICacheService cacheService,
        ICallAutomationService callAutomationService,
        IConfiguration configuration,
        ILogger<ChatService> logger)
    {
        this.identityService = identityService;
        this.cacheService = cacheService;
        this.callAutomationService = callAutomationService;
        this.configuration = configuration;
        this.logger = logger;
        this.acsEndpoint = this.configuration["AcsEndpoint"] ?? "";
        this.acsOutboundCallerId = this.configuration["AcsPhoneNumber"] ?? "";
        ArgumentException.ThrowIfNullOrEmpty(acsEndpoint);

        botUserId = identityService.GetNewUserId();
        cacheService.UpdateCache("BotUserId", botUserId);
    }

    public async Task<ChatClientResponse> GetOrCreateCallConversation()
    {
        var userId = cacheService.GetCache("UserId");
        var token = cacheService.GetCache("Token");
        var threadId = cacheService.GetCache("ThreadId");

        // 1. Create and cache new identity for customer if needed
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            (userId, token) = await identityService.GetNewUserIdAndToken();
            cacheService.UpdateCache("UserId", userId);
            cacheService.UpdateCache("Token", token);
        }

        // 2. Prepare new chat conversation as bot
        (var chatThreadClient, threadId) = await GetOrCreateBotChatThreadClient(threadId ?? null);
        cacheService.UpdateCache("ThreadId", threadId);

        // 3. Invite customer to conversation with bot
        chatThreadClient.AddParticipant(new ChatParticipant(new CommunicationUserIdentifier(userId))
        {
            DisplayName = "Customer"
        });

        return new ChatClientResponse
        {
            ThreadId = threadId,
            Token = token,
            Identity = userId,
            EndpointUrl = acsEndpoint,
            BotUserId = botUserId,
        };
    }

    public async Task HandleEvent(AcsChatMessageReceivedInThreadEventData chatEvent)
    {
        var eventSender = chatEvent.SenderCommunicationIdentifier.RawId;
        var eventMessage = chatEvent.MessageBody;
        var eventThreadId = chatEvent.ThreadId;
        var eventSenderType = chatEvent.Metadata.GetValueOrDefault("SenderType");
        // TODO : Implement event handling
        await Task.FromResult(true);
    }

    public async Task<List<ChatHistory>> GetChatHistory(string threadId)
    {
        (var chatThreadClient, _) = await GetOrCreateBotChatThreadClient(threadId);
        return GetFormattedChatHistory(chatThreadClient);
    }

    private List<ChatHistory> GetFormattedChatHistory(ChatThreadClient chatThreadClient)
    {
        List<ChatHistory> chatHistoryList = GetChatHistoryWithThreadClient(chatThreadClient);
        return chatHistoryList.OrderBy(x => x.CreatedOn).ToList();
    }

    private async Task<(ChatThreadClient, string)> GetOrCreateBotChatThreadClient(string? threadId = null)
    {
        var botToken = await identityService.GetTokenForUserId(botUserId);
        ChatClient chatClient = new ChatClient(new Uri(acsEndpoint), new CommunicationTokenCredential(botToken));

        string? chatThreadId = threadId;

        if (string.IsNullOrEmpty(chatThreadId))
        {
            var botParticipant = new ChatParticipant(new CommunicationUserIdentifier(id: botUserId))
            {
                DisplayName = "Bot"
            };
            CreateChatThreadResult createChatThreadResult = await chatClient.CreateChatThreadAsync(
                topic: "Customer Support",
                new[] { botParticipant });
            chatThreadId = createChatThreadResult.ChatThread.Id;
        }

        return (chatClient.GetChatThreadClient(chatThreadId), chatThreadId);
    }

    private static bool TryGetPhoneNumber(string message, out string phoneNumber)
    {
        Regex regex = new(PSTNRegex);
        MatchCollection matches = regex.Matches(message);
        if (matches.Count > 0)
        {
            phoneNumber = matches[0].Value;
            if (!phoneNumber.StartsWith("+"))
            {
                phoneNumber = $"+{phoneNumber}";
            }
            phoneNumber = phoneNumber.Replace(" ", "");
            phoneNumber = phoneNumber.Replace("-", "");
            return true;
        }
        phoneNumber = "";
        return false;
    }

    public static List<ChatHistory> GetChatHistoryWithThreadClient(ChatThreadClient chatThreadClient)
    {
        var chatMessages = chatThreadClient.GetMessages();
        List<ChatHistory> chatHistoryList = new();
        foreach (var chatMessage in chatMessages)
        {
            if (chatMessage.Sender?.RawId is not null)
            {
                ChatHistory chatHistory = new()
                {
                    MessageId = chatMessage.Id,
                    Content = chatMessage.Content?.Message,
                    SenderId = chatMessage.Sender?.RawId,
                    CreatedOn = chatMessage.CreatedOn,
                    MessageType = "chat",
                    ContentType = chatMessage.Type.ToString(),
                    SenderDisplayName = !string.IsNullOrEmpty(chatMessage.SenderDisplayName) ? chatMessage.SenderDisplayName : "Bot",
                };
                chatHistoryList.Add(chatHistory);
            }
        }

        return chatHistoryList;
    }
}