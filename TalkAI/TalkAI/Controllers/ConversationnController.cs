using BLL.Interface;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/conversationn")]
public class ConversationnController : ControllerBase
{
    private readonly IAzureLanguageService _languageService;
    private readonly IAudioRecorderService _recorderService;

    public ConversationnController(
        IAzureLanguageService languageService,
        IAudioRecorderService recorderService)
    {
        _languageService = languageService;
        _recorderService = recorderService;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartConversation([FromQuery] int topicId)
    {

        // Xử lý
        var response = await _languageService.ProcessRealTimeConversationAsync(topicId);

        return Ok(new
        {
            Text = response.AudioResponse,
            Audio = Convert.ToBase64String(response.AudioResponse),
          
        });
    }
}