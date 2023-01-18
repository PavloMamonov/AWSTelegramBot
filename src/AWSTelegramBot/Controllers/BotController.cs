using AWSTelegramBot.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace AWSTelegramBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IUpdateService _updateService;

        public BotController(IUpdateService updateService)
        {
            _updateService = updateService;
        }

        [HttpPost]
        public async Task<IActionResult> HandleUpdate([FromBody] Update update,
            CancellationToken cancellationToken = default)
        {
            await _updateService.HandleUpdate(update, cancellationToken);
            return Ok();
        }
    }
}
