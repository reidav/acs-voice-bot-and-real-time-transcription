#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Api.Agents.Tools;

public interface ITool {
    ConversationFunctionTool Definition { get; }

    Task<string> ExecuteAsync(string jsonParams);
}