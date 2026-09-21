using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.AspNetCore.Mvc;

namespace TeamsTimeBot.Api.Controllers;

[ApiController]
[Route("api/messages")]
public class MessagesController : ControllerBase
{
    private readonly CloudAdapter _adapter;
    private readonly IBot _bot;

    public MessagesController(CloudAdapter adapter, IBot bot)
    {
        _adapter = adapter;
        _bot = bot;
    }

    [HttpPost]
    public async Task PostAsync(CancellationToken cancellationToken)
    {
        await _adapter.ProcessAsync(
            Request,
            Response,
            _bot,
            cancellationToken);
    }
}