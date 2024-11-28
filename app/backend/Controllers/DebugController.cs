namespace Api.Controllers;

[Route("api/debug")]
[ApiController]
/* These API routes are for developer debug purposes. Not used by sample webapp */
public class DebugController(
    ICacheService cacheService,
    ICallAutomationService callAutomationService,
    IConfiguration configuration) : Controller
{
    private readonly ICacheService cacheService = cacheService;
    private readonly ICallAutomationService callAutomationService = callAutomationService;
    private readonly IConfiguration configuration = configuration;

    [HttpGet]
    [Route("clearCache")]
    public ActionResult ClearCache()
    {
        var result = cacheService.ClearCache();
        return Ok(result);
    }

    [HttpPost]
    [Route("callToPstn")]
    public async Task<IActionResult> CreateCall(string targetPSTNNumber = "", string threadId = "")
    {
        string callerId = configuration["AcsPhoneNumber"] ?? "";
        return Ok(await callAutomationService.CreateCallAsync(callerId, targetPSTNNumber, threadId));
    }
}