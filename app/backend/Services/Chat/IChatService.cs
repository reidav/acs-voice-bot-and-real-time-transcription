namespace Api.Services.Chat;

public interface IChatService
{
    Task<ChatClientResponse> GetOrCreateCallConversation();

    Task<List<ChatHistory>> GetChatHistory(string threadId);

    Task HandleEvent(AcsChatMessageReceivedInThreadEventData chatMessageReceivedEvent);
}