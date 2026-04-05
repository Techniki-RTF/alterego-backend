using AlterEgo.Api.Dtos;
using AlterEgo.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlterEgo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LlmController : ControllerBase
{
    private readonly ILlmService _llmService;
    private readonly ILogger<LlmController> _logger;

    public LlmController(ILlmService llmService, ILogger<LlmController> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(LlmResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Generate([FromBody] LlmRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new ErrorResponse("Message cannot be empty"));
        }

        _logger.LogInformation("LLM generation requested");

        try
        {
            var response = await _llmService.GenerateTextAsync(request.Message, cancellationToken);
            return Ok(new LlmResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate LLM response");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse("Failed to generate response from AI service"));
        }
    }
}
