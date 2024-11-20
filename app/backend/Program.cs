using System.ComponentModel.DataAnnotations;
using Api.Services;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddSingleton<IAcsEventHandler, AcsEventHandler>();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration.GetSection("Settings"));
builder.Services.AddSingleton<ITranscriptService, TranscriptService>();

var app = builder.Build();

var configuration = app.Services.GetRequiredService<IConfiguration>();
var transcriptService = app.Services.GetRequiredService<ITranscriptService>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/api/events/incoming-call", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    IAcsEventHandler acsEventHandler,
    ILogger<Program> logger) =>
{
    return await acsEventHandler.IncomingCallAsync(eventGridEvents);
});

app.MapPost("/api/events/callbacks/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId,
    IAcsEventHandler acsEventHandler,
    ILogger<Program> logger) =>
{
    return acsEventHandler.Callback(cloudEvents, contextId, callerId);
});

app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws/transcript")
    {
        Console.WriteLine("Transcript voice request received");
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        transcriptService.TranscriptCompleted += TranscriptService_TranscriptCompleted;

        await transcriptService.ProcessRequest(webSocket);
    }
    else if (context.Request.Path == "/ws/voice")
    {
        Console.WriteLine("Ws voice request received");
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var mediaService = new AcsMediaStreamingHandler(webSocket, configuration);

        // Set the single WebSocket connection
        await mediaService.ProcessWebSocketAsync();
    }
    else if (context.Request.Path == "/ws")
    {
        Console.WriteLine("Ws request received");
    }
    else
    {
        await next(context);
    }
});

async void TranscriptService_TranscriptCompleted(object? sender, TranscriptEventArgs e)
{
    if (e.Data.Text.Contains("sandra", StringComparison.InvariantCultureIgnoreCase))
    {
        var callbackUriHost = configuration.GetValue<string>("CallbackUriHost");
        var callAutomationClient = new CallAutomationClient(
            configuration.GetValue<string>("AcsConnectionString")
        );

        try
        {
            var con = callAutomationClient.GetCallConnection(e.Metadata.CallConnectionId);
            var media = con.GetCallMedia();
            await media.StartMediaStreamingAsync();
        }
        catch (Exception)
        { // TODO : Manage exception better
        }
    }
 
    if (e.Data.Text.Contains("silence", StringComparison.InvariantCultureIgnoreCase))
    {
        var callbackUriHost = configuration.GetValue<string>("CallbackUriHost");
        var callAutomationClient = new CallAutomationClient(
            configuration.GetValue<string>("AcsConnectionString")
        );

        try
        {
            var con = callAutomationClient.GetCallConnection(e.Metadata.CallConnectionId);
            var media = con.GetCallMedia();
            await media.StopMediaStreamingAsync();
        }
        catch (Exception)
        { // TODO : Manage exception better
        }
    }
}

app.Run();
