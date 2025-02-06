using Microsoft.AspNetCore.Mvc;
using BLL.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;
using BLL.Services;

namespace TalkAI.Controllers
{
    [Route("api/azure-language")]
    [ApiController]
    public class AzureLanguageController : ControllerBase
    {
        private readonly IAzureLanguageService _languageService;
        private readonly ILogger<AzureLanguageController> _logger;

        public AzureLanguageController(
            IAzureLanguageService languageService,
            ILogger<AzureLanguageController> logger)
        {
            _languageService = languageService;
            _logger = logger;
        }

        [HttpPost("start-topic")]
        public async Task<IActionResult> StartTopic([FromBody] StartTopicRequest request)
        {
            try
            {
                var response = await _languageService.StartTopicAsync(request.TopicId);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting topic {TopicId}", request.TopicId);
                return StatusCode(500, "Failed to start topic");
            }
        }
        [HttpPost("end-conversation")]
        public async Task<IActionResult> EndConversation()
        {
            try
            {
                var result = await _languageService.EndConversationAsync();

                if (result.Overall <= 0 || string.IsNullOrWhiteSpace(result.Suggestions))
                {
                    _logger.LogError("Invalid evaluation result: {@Result}", result);
                    return StatusCode(500, new { error = "Evaluation failed. Check OpenAI response format." });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical evaluation failure");
                return StatusCode(500, new { error = "Evaluation service error: " + ex.Message });
            }
        }
        [HttpPost("process-conversation")]
        public async Task<IActionResult> ProcessConversation([FromBody] ProcessConversationRequest request)
        {
            try
            {
                var response = await _languageService.ProcessConversationAsync(request.UserMessage);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing conversation message");
                return StatusCode(500, "Failed to process message");
            }
        }
    }

    public class StartTopicRequest
    {
        public int TopicId { get; set; }
    }

    public class ProcessConversationRequest
    {
        public string UserMessage { get; set; }
        public string CurrentTopic { get; set; }
        public List<MessageHistory> ConversationHistory { get; set; }
    }

    public class MessageHistory
    {
        public string Text { get; set; }
        public bool IsBot { get; set; }
    }
}