namespace Api.Agents;

#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public interface IVoiceAgent {
    string Instructions { get; }
    
    ConversationVoice Voice { get; }

    bool IsTranscriptEnabled  { get; }

    IList<ITool> Tools { get; }
}