namespace Api.Services.Transcription;

public interface ITranscriptionService
{
    Task TranscribeVoiceMessageToChat(string userId, string threadId, string text, string displayName);
}