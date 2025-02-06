using Microsoft.AspNetCore.Mvc;
using BLL.Interface;
using System.Threading.Tasks;
using DAL.Entities;
using BLL.Interfaces;
using BLL.Services;

namespace TalkAI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationController : ControllerBase
    {
        private readonly IAzureLanguageService _languageService;
        private readonly IAzureSpeechService _speechService;

        public ConversationController(IAzureLanguageService languageService, IAzureSpeechService speechService)
        {
            _languageService = languageService;
            _speechService = speechService;
        }

        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] MessageRequest request)
        {
            var response = await _languageService.ProcessConversationAsync(request.Message);
            return Ok(response);
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartConversation([FromBody] StartConversationRequest request)
        {
            try
            {
                var response = await _languageService.StartTopicAsync(request.TopicId);

                // Convert bot response to speech
                var audioData = await _speechService.ConvertTextToSpeech(response.BotResponse);

                return Ok(new
                {
                    response.CurrentTopic,
                    response.BotResponse,
                    response.TurnsRemaining,
                    audioBase64 = Convert.ToBase64String(audioData)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        [HttpPost("process")]
        public async Task<IActionResult> ProcessConversation([FromBody] ConversationProcessRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(request.Text))
                {
                    return BadRequest("Text must be provided");
                }

                if (!Guid.TryParse(request.UserId, out _))
                {
                    return BadRequest("Invalid user ID format");
                }

                // Set the current topic based on the TopicId from the request
                _languageService.SetCurrentTopic(request.TopicId); // Assuming you have a method to set the current topic

                // Process conversation with text
                var response = await _languageService.ProcessConversationAsync(request.Text);

                // Return response
                return Ok(new
                {
                    response.IsComplete,
                    response.BotResponse,
                    response.Evaluation,
                    response.TurnsRemaining,
                    UserMessage = request.Text
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }
        [HttpGet("topics")]
        public IActionResult GetTopics()
        {
            var topics = new List<Topicc>
            {
                new Topicc
                {
                    Id = "1",
                    Name = "Daily Life and Routines",
                    Description = "Discuss your daily activities, habits, and lifestyle",
                    CharacterRole = "friendly roommate"
                },
                new Topicc
                {
                    Id = "2",
                    Name = "Travel and Cultural Experiences",
                    Description = "Share travel stories and cultural encounters",
                    CharacterRole = "experienced traveler"
                },
                new Topicc
                {
                    Id = "3",
                    Name = "Technology and Innovation",
                    Description = "Explore modern technology trends and innovations",
                    CharacterRole = "tech enthusiast"
                },
                new Topicc
                {
                    Id = "4",
                    Name = "Education and Career Development",
                    Description = "Discuss learning experiences and career goals",
                    CharacterRole = "career coach"
                }
            };

            return Ok(topics);
        }

        public class ConversationProcessRequest
        {
            public string UserId { get; set; }
            public string Text { get; set; }
            public int TopicId { get; set; }
        }
        public class Topicc
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string CharacterRole { get; set; }
        }
        public class StartConversationRequest
        {
            public int TopicId { get; set; }
        }
    }
}