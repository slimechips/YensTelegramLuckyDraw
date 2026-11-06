using Microsoft.AspNetCore.Mvc;
using YensTelegramLuckyDraw.Services;

namespace YensTelegramLuckyDraw.Controllers;

[ApiController]
[Route("api/lateral-thinking")]
public class LateralThinkingController(OpenAiService openAiService) : ControllerBase
{
  [HttpPost]
  public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(request.Message))
    {
      return BadRequest("Message is required.");
    }

    var response = await openAiService.ChatAsync(request.Message, cancellationToken);
    return Ok(new { response });
  }

  public sealed record ChatRequest(string Message);
}
