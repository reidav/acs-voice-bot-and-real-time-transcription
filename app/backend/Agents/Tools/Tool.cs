#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Api.Agents.Tools;

public abstract class Tool : ITool {
    public abstract ConversationFunctionTool Definition { get; }

    public abstract Task<string> ExecuteAsync(string input);
}