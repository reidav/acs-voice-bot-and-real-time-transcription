using System.Collections.Concurrent;

[Route("agents")]
public class WebSocketController : ControllerBase
{
    private readonly ILogger logger;
    private readonly ITranscriptReceiver transcriptReceiver;
    private readonly IConfiguration configuration;
    private readonly ConcurrentDictionary<string, WebSocket> connections = new();

    public WebSocketController(
        IConfiguration configuration,
        ITranscriptReceiver transcriptReceiver,
        ILogger<WebSocketController> logger)
    {
        this.logger = logger;
        this.transcriptReceiver = transcriptReceiver;
        this.configuration = configuration;

        this.transcriptReceiver.TranscriptCompleted += async (sender, args) =>
            {
                logger.LogInformation("Transcript completed event received ...");
                await Task.Run(async () =>
                {
                    foreach (var ws in connections)
                    {
                        logger.LogInformation($"Sending transcript to WebSocket connection with session ID: {ws.Key}");
                        if (ws.Value?.State == WebSocketState.Open)
                        {
                            logger.LogInformation($"Transcript: {args.Data.Text}");
                            byte[] jsonBytes = Encoding.UTF8.GetBytes(args.Data.Text);
                            await ws.Value!.SendAsync(new ArraySegment<byte>(jsonBytes), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
                        }
                    }
                });
            };
    }

    [Route("webapp")]
    public async Task GetWebApp()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            string sessionId = Guid.NewGuid().ToString("N");
            connections.TryAdd(sessionId, webSocket);
            logger.LogInformation($"WebSocket connection established with session ID: {sessionId}");
            while (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.Closed)
                await webSocket.ReceiveAsync(new ArraySegment<byte>(new byte[1024]), CancellationToken.None);
            logger.LogInformation($"WebSocket connection closed with session ID: {sessionId}");
            connections.TryRemove(sessionId, out _);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    [Route("transcript")]
    public async Task GetTranscript()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await transcriptReceiver.ProcessRequest(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    [Route("haircut-appointment")]
    public async Task GetHaircutAgent()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            this.logger.LogInformation("WebSocket voice request received");
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var haircutAppointmentAgent = new HaircutAppointmentVoiceAgent(webSocket, configuration);
            await haircutAppointmentAgent.Process();
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    [Route("customer")]
    public async Task GetCustomerAgent()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            this.logger.LogInformation("WebSocket voice request received");
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var customerAgent = new CustomerVoiceAgent(webSocket, configuration);
            await customerAgent.Process();
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}