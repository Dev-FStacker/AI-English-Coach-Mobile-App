using Microsoft.AspNetCore.Mvc;
using BLL.Interfaces;
using System.Net.Mime;
using DAL.Entities;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpeechController : ControllerBase
    {
        private readonly IAzureSpeechService _speechService;
        private readonly ILogger<SpeechController> _logger;

        public SpeechController(IAzureSpeechService speechService, ILogger<SpeechController> logger)
        {
            _speechService = speechService;
            _logger = logger;
        }
        /// <summary>
        /// Convert speech to text from audio file
        /// </summary>
        /// <param name="file">The audio file to convert</param>
        /// <returns>The converted text</returns>
       
        [HttpPost("speech-to-text-from-blob")]
        public async Task<IActionResult> SpeechToTextFromBlob([FromBody] BlobRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.BlobUrl))
                    return BadRequest("Blob URL is required");

                var text = await _speechService.ConvertSpeechToTextFromBlobStorage(request.BlobUrl);

                // Lưu conversation
                var userId = Guid.NewGuid();
                await _speechService.SaveConversation(userId, request.BlobUrl, text);

                return Ok(new
                {
                    text = text,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audio from blob");
                return StatusCode(500, "An error occurred processing the audio");
            }
        }
    }

    public class BlobRequest
    {
        public string BlobUrl { get; set; }
    }
} 