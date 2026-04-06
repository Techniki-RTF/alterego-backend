using Microsoft.AspNetCore.Mvc;

namespace AlterEgo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(StatusResponse), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        return Ok(new StatusResponse("ok", DateTimeOffset.UtcNow));
    }
}

public record StatusResponse(string Status, DateTimeOffset ServerTime);
