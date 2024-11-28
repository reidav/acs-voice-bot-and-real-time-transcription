using Microsoft.Extensions.Logging.Abstractions;

namespace Api.Controllers;

[Route("api")]
[ApiController]
public class EventsController(
    ICacheService cacheService,
    IConfiguration configuration,
    ICallAutomationService callAutomationService,
    ILogger<EventsController> logger) : Controller
{
    private readonly ILogger logger = logger;
    private readonly ICacheService cacheService = cacheService;
    private readonly IConfiguration configuration = configuration;
    private readonly EventConverter eventConverter = new EventConverter();
     
    private readonly ICallAutomationService callAutomationService = callAutomationService;

    /* Route for Azure Communication Service eventgrid webhooks */
    [HttpPost]
    [Route("events")]
    public async Task<IActionResult> Handle([FromBody] EventGridEvent[] eventGridEvents)
    {
        foreach (var eventGridEvent in eventGridEvents)
        {
            if (eventGridEvent.TryGetSystemEventData(out object eventData))
            {
                if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
                {
                    var responseData = new SubscriptionValidationResponse
                    {
                        ValidationResponse = subscriptionValidationEventData.ValidationCode
                    };

                    return Ok(responseData);
                }
            }

            if (eventData is AcsIncomingCallEventData incomingCallEventData)
            {
                await this.callAutomationService.AnswerCallAsync(incomingCallEventData);
            }
        }

        return Ok();
    }


    /* Route for CallAutomation in-call event callbacks */
    [HttpPost]
    [Route("callbacks/{contextId}")]
    public async Task<IActionResult> Handle([FromBody] CloudEvent[] cloudEvents, [FromRoute] string contextId, [FromQuery] string callerId)
    {
        foreach (var cloudEvent in cloudEvents)
        {
            CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
            logger.LogInformation(
                "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId} {event}",
                parsedEvent.GetType(),
                parsedEvent.CallConnectionId,
                parsedEvent.ServerCallId,
                parsedEvent.ToString());

            switch (parsedEvent)
            {
                case CallConnected callConnected:
                    await callAutomationService.HandleEvent(callConnected);
                    break;

                default:
                    break;
            }
        }

        return Ok();
    }
}