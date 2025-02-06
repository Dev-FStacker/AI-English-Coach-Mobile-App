using BLL.Interface;
using BLL.Services;
using DAL.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace TalkAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IAzureLanguageService _languageService;

        public ChatController(
            IHubContext<ChatHub> hubContext,
            IAzureLanguageService languageService)
        {
            _hubContext = hubContext;
            _languageService = languageService;
        }




        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] MessageRequest request)
        {
            if (string.IsNullOrEmpty(request?.Message))
            {
                return BadRequest("Message cannot be empty");
            }

            try
            {
                var response = await _languageService.ProcessConversationAsync(request.Message);
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", new
                {
                    message = response.BotResponse,
                    isBot = true,
                    timestamp = DateTime.UtcNow
                });
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("test-connection")]
        public IActionResult TestConnection()
        {
            return Ok(new { status = "Connected" });
        }
    

    }


}