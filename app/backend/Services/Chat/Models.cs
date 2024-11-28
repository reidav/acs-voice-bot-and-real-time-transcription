namespace Api.Services.Chat;

public class ChatHistory
{
    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("senderId")]
    public string? SenderId { get; set; }

    [JsonPropertyName("messageType")]
    public string? MessageType { get; set; }

    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    [JsonPropertyName("senderDisplayName")]
    public string? SenderDisplayName { get; set; }

    [JsonPropertyName("createdOn")]
    public DateTimeOffset CreatedOn { get; set; }
}

public class ChatClientResponse
    {
        [JsonPropertyName("threadId")]
        public string? ThreadId { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("identity")]
        public string? Identity { get; set; }

        [JsonPropertyName("endpointUrl")]
        public string? EndpointUrl { get; set; }

        [JsonPropertyName("messageId")]
        public string? MessageId { get; set; }

        [JsonPropertyName("botUserId")]
        public string? BotUserId { get; set; }
    }