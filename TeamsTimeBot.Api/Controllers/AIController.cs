using Microsoft.AspNetCore.Mvc;

using TeamsTimeBot.Api.Models;
using TeamsTimeBot.Api.Services;

namespace TeamsTimeBot.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AIController : ControllerBase
{
    private readonly AIService _aiService;

    public AIController(AIService aiService)
    {
        _aiService = aiService;
    }

    [HttpGet("test")]
    public async Task<IActionResult> Test()
    {
        var messages = new List<ConversationMessage>
        {
            new ConversationMessage
            {
                Role = "user",
                Content = "Zacząłem pracę"
            },

            new ConversationMessage
            {
                Role = "assistant",
                Content = "Jasne. Nad którym zadaniem pracujesz?"
            },

            new ConversationMessage
            {
                Role = "user",
                Content = "Frontend"
            }
        };

        var userAzureId = "test-user";

        var response =
            await _aiService.GetResponseAsync(
                messages,
                userAzureId);

        return Ok(response);
    }
}

