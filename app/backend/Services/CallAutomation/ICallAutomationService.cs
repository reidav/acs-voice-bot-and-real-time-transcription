namespace Api.Services.CallAutomation;

public interface ICallAutomationService
{
    Task AnswerCallAsync(AcsIncomingCallEventData eventData);

    Task<CreateCallResult> CreateCallAsync(string callerId, string targetParticipant, string threadId = "");

    Task HandleEvent(CallConnected callConnected);

    CallConnection GetCallConnection(string callConnectionId);
}