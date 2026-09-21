using Microsoft.AspNetCore.Mvc;
using TeamsTimeBot.Api.Services;

namespace TeamsTimeBot.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly BotEngine _botEngine;

    public TestController(BotEngine botEngine)
    {
        _botEngine = botEngine;
    }

    [HttpPost("conversation")]
    public async Task<IActionResult> RunConversationTest()
    {
        const string userId = "24940820-21cb-4c70-bb2c-9b088075bb35";

        var messages = new[]
        {
            "hej",
            "utworz zadanie frontend",
            "opis: przygotowanie nowego widoku dashboardu",

            "dodaj jeszcze zadanie backend api",
            "opis: endpointy do zarzadzania uzytkownikami",

            "zacznij robic frontend",
            "stop",

            "zacznij frontend",
            "pracuje teraz nad backend api",

            "dodaj komentarz: trzeba jeszcze ogarnac walidacje",

            "zmien opis backend api",
            "endpointy users i autoryzacja",

            "zatrzymaj czas",

            "zacznij znow frontend",
            "koniec pracy na frontend",

            "oznacz frontend jako skonczony",

            "utworz zadanie testy",
            "opis: testy integracyjne backendu",

            "dodaj komentarz do testy: trzeba pokryc edge case",
            "dodaj jeszcze komentarz: sprawdzic tez bledy 500",

            "zacznij testy",
            "pracowalem 45 minut nad testy",
            "stop"
        };

        var results = new List<object>();

        foreach (var message in messages)
        {
            var response =
                await _botEngine.HandleAsync(
                    userId,
                    message);

            results.Add(new
            {
                User = message,
                Bot = response
            });
        }

        return Ok(results);
    }
}